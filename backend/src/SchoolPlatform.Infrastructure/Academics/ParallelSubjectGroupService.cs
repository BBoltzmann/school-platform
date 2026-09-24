using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Academics;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Domain.Timetabling;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Academics;

public sealed class ParallelSubjectGroupService(
    SchoolPlatformDbContext database,
    ITenantContext tenantContext) : IParallelSubjectGroupService
{
    public async Task<IReadOnlyCollection<ParallelSubjectGroupResult>> ListAsync(Guid classGroupId, Guid academicSessionId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        await EnsureClassAndSessionAsync(classGroupId, academicSessionId, cancellationToken);
        var groups = await database.ParallelSubjectGroups.AsNoTracking().Include(x => x.Members).ThenInclude(x => x.ClassSubject).ThenInclude(x => x.Subject).Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.AcademicSessionId == academicSessionId && x.IsActive).ToListAsync(cancellationToken);
        var subjectIds = groups.SelectMany(x => x.Members).Select(x => x.ClassSubject.SubjectId).Distinct().ToArray();
        var requirements = await database.ClassSubjectRequirements.AsNoTracking().Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.AcademicSessionId == academicSessionId && x.IsActive && subjectIds.Contains(x.SubjectId)).ToDictionaryAsync(x => x.SubjectId, cancellationToken);
        var assignments = await database.TeachingAssignments.AsNoTracking().Include(x => x.StaffMember).Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.AcademicSessionId == academicSessionId && x.IsActive && subjectIds.Contains(x.SubjectId)).ToListAsync(cancellationToken);
        return groups.Select(x => new ParallelSubjectGroupResult(x.Id, x.AcademicSessionId, x.ClassGroupId, x.DisplayName, x.IsActive, x.Members.Select(m => requirements.GetValueOrDefault(m.ClassSubject.SubjectId)?.PeriodsPerWeek ?? 0).FirstOrDefault(), x.Members.Select(m => { var assignment = assignments.FirstOrDefault(a => a.SubjectId == m.ClassSubject.SubjectId); var staff = assignment?.StaffMember; var name = staff is null ? null : string.Join(" ", new[] { staff.FirstName, staff.MiddleName, staff.LastName }.Where(v => !string.IsNullOrWhiteSpace(v))); return new ParallelSubjectGroupMemberResult(m.ClassSubjectId, m.ClassSubject.SubjectId, m.ClassSubject.Subject.Name, m.ClassSubject.Subject.Code, requirements.GetValueOrDefault(m.ClassSubject.SubjectId)?.PeriodsPerWeek ?? 0, name); }).ToArray())).ToArray();
    }

    public async Task<ParallelSubjectGroupResult> CreateAsync(Guid classGroupId, SaveParallelSubjectGroupRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        await EnsureClassAndSessionAsync(classGroupId, request.AcademicSessionId, cancellationToken);
        var classSubjects = await ValidateMembersAsync(classGroupId, request, null, cancellationToken);
        var group = new ParallelSubjectGroup(tenantId, request.AcademicSessionId, classGroupId, request.DisplayName);
        database.ParallelSubjectGroups.Add(group);
        foreach (var classSubject in classSubjects) group.Members.Add(new ParallelSubjectGroupMember(tenantId, group.Id, classSubject.Id));
        await database.SaveChangesAsync(cancellationToken);
        return (await ListAsync(classGroupId, request.AcademicSessionId, cancellationToken)).Single(x => x.Id == group.Id);
    }

    public async Task<ParallelSubjectGroupResult> UpdateAsync(Guid classGroupId, Guid groupId, SaveParallelSubjectGroupRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var group = await database.ParallelSubjectGroups.Include(x => x.Members).SingleOrDefaultAsync(x => x.Id == groupId && x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.IsActive, cancellationToken) ?? throw new InvalidOperationException("Parallel subject group was not found.");
        if (group.AcademicSessionId != request.AcademicSessionId) throw new InvalidOperationException("The academic session cannot be changed for this group.");
        var classSubjects = await ValidateMembersAsync(classGroupId, request, groupId, cancellationToken);
        group.UpdateDisplayName(request.DisplayName);
        database.ParallelSubjectGroupMembers.RemoveRange(group.Members);
        foreach (var classSubject in classSubjects) database.ParallelSubjectGroupMembers.Add(new ParallelSubjectGroupMember(tenantId, group.Id, classSubject.Id));
        await database.SaveChangesAsync(cancellationToken);
        return (await ListAsync(classGroupId, request.AcademicSessionId, cancellationToken)).Single(x => x.Id == group.Id);
    }

    public async Task DeleteAsync(Guid classGroupId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await database.ParallelSubjectGroups
            .Include(x => x.Members)
            .SingleOrDefaultAsync(x => x.Id == groupId && x.TenantId == tenantContext.TenantId && x.ClassGroupId == classGroupId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Parallel subject group was not found.");
        if (await database.GeneratedTimetableEntries.AnyAsync(x => x.TenantId == tenantContext.TenantId && x.ParallelSubjectGroupId == groupId, cancellationToken))
        {
            throw new InvalidOperationException("This parallel group is used by a generated timetable. Reset the generated timetable before removing the group.");
        }
        database.ParallelSubjectGroupMembers.RemoveRange(group.Members);
        group.Deactivate();
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetAsync(Guid classGroupId, Guid academicSessionId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        await EnsureClassAndSessionAsync(classGroupId, academicSessionId, cancellationToken);

        var groups = await database.ParallelSubjectGroups
            .Include(x => x.Members)
            .Where(x =>
                x.TenantId == tenantId &&
                x.ClassGroupId == classGroupId &&
                x.AcademicSessionId == academicSessionId &&
                x.IsActive)
            .ToListAsync(cancellationToken);

        if (groups.Count == 0)
        {
            return;
        }

        var groupIds = groups.Select(group => group.Id).ToArray();
        var hasGeneratedHistory = await database.GeneratedTimetableEntries.AnyAsync(
            x => x.TenantId == tenantId && x.ParallelSubjectGroupId.HasValue && groupIds.Contains(x.ParallelSubjectGroupId.Value),
            cancellationToken);

        // Deactivation is safe even when history references the group. Keep its
        // members in that case so historical timetable relationships remain
        // descriptive; when there is no history, remove the configuration rows.
        if (!hasGeneratedHistory)
        {
            database.ParallelSubjectGroupMembers.RemoveRange(groups.SelectMany(x => x.Members));
        }
        foreach (var group in groups)
        {
            group.Deactivate();
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<SchoolPlatform.Domain.Academics.ClassSubject>> ValidateMembersAsync(Guid classGroupId, SaveParallelSubjectGroupRequest request, Guid? currentGroupId, CancellationToken cancellationToken)
    {
        var ids = request.ClassSubjectIds.Distinct().ToArray();
        if (ids.Length < 2) throw new InvalidOperationException("A parallel subject group must contain at least two subjects.");
        var tenantId = tenantContext.TenantId;
        var classSubjects = await database.ClassSubjects.Include(x => x.Subject).Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.IsActive && ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (classSubjects.Count != ids.Length) throw new InvalidOperationException("Every selected subject must be offered by this class.");
        var subjectIds = classSubjects.Select(cs => cs.SubjectId).ToHashSet();
        var requirements = await database.ClassSubjectRequirements.Where(x => x.TenantId == tenantId && x.AcademicSessionId == request.AcademicSessionId && x.ClassGroupId == classGroupId && x.IsActive && subjectIds.Contains(x.SubjectId)).ToListAsync(cancellationToken);
        var missingRequirementNames = classSubjects
            .Where(cs => requirements.All(requirement => requirement.SubjectId != cs.SubjectId))
            .Select(cs => cs.Subject.Name)
            .Distinct()
            .ToArray();
        if (missingRequirementNames.Length > 0)
        {
            var subjects = string.Join(", ", missingRequirementNames);
            throw new InvalidOperationException(
                $"{subjects} {(missingRequirementNames.Length == 1 ? "has" : "have")} no weekly period requirement. Set periods/week before creating this parallel group.");
        }
        if (requirements.Select(x => x.PeriodsPerWeek).Distinct().Count() != 1) throw new InvalidOperationException("Subjects in a parallel group must have matching weekly period requirements.");
        var conflicts = await database.ParallelSubjectGroupMembers
            .Include(x => x.ParallelSubjectGroup)
            .ThenInclude(x => x.Members)
            .ThenInclude(x => x.ClassSubject)
            .ThenInclude(x => x.Subject)
            .Where(x => x.TenantId == tenantId && x.ParallelSubjectGroup.ClassGroupId == classGroupId && x.ParallelSubjectGroup.AcademicSessionId == request.AcademicSessionId && x.ParallelSubjectGroup.IsActive && (!currentGroupId.HasValue || x.ParallelSubjectGroupId != currentGroupId.Value) && ids.Contains(x.ClassSubjectId))
            .ToListAsync(cancellationToken);
        if (conflicts.Count > 0)
        {
            var conflictMessages = conflicts
                .GroupBy(x => x.ClassSubjectId)
                .Select(group =>
                {
                    var conflict = group.First();
                    var configuredName = conflict.ParallelSubjectGroup.DisplayName;
                    var fallbackName = string.Join(" / ", conflict.ParallelSubjectGroup.Members.Select(member => member.ClassSubject.Subject.Name));
                    var groupName = string.IsNullOrWhiteSpace(configuredName) ? fallbackName : configuredName;
                    var memberNames = string.Join(", ", conflict.ParallelSubjectGroup.Members.Select(member => member.ClassSubject.Subject.Name));
                    return $"{conflict.ClassSubject.Subject.Name} already belongs to '{groupName}' with {memberNames}.";
                });
            throw new InvalidOperationException(string.Join(" ", conflictMessages));
        }
        return classSubjects;
    }

    private async Task EnsureClassAndSessionAsync(Guid classGroupId, Guid academicSessionId, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        if (!await database.ClassGroups.AnyAsync(x => x.Id == classGroupId && x.TenantId == tenantId && x.IsActive, cancellationToken)) throw new InvalidOperationException("Class was not found.");
        if (!await database.AcademicSessions.AnyAsync(x => x.Id == academicSessionId && x.TenantId == tenantId && x.IsActive, cancellationToken)) throw new InvalidOperationException("Academic session was not found.");
    }

}
