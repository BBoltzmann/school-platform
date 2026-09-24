using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Academics;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Academics;

public sealed class ClassSubjectService(SchoolPlatformDbContext database, ITenantContext tenantContext) : IClassSubjectService
{
    public async Task<ClassSubjectOfferingResult> GetAsync(Guid classGroupId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var classGroup = await database.ClassGroups.AsNoTracking().SingleOrDefaultAsync(x => x.Id == classGroupId && x.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Class was not found.");
        var assignedIds = classGroup.UsesCustomSubjectOffering ? Array.Empty<Guid>() : await database.TeachingAssignments.AsNoTracking().Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.IsActive).Select(x => x.SubjectId).Distinct().ToArrayAsync(cancellationToken);
        var subjects = classGroup.UsesCustomSubjectOffering
            ? await database.ClassSubjects.AsNoTracking().Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.IsActive && x.Subject.IsActive).OrderBy(x => x.Subject.Name).Select(x => new ClassSubjectResult(x.Id, x.SubjectId, x.Subject.Name, x.Subject.Code)).ToListAsync(cancellationToken)
            : await database.Subjects.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsActive && assignedIds.Contains(x.Id)).OrderBy(x => x.Name).Select(x => new ClassSubjectResult(Guid.Empty, x.Id, x.Name, x.Code)).ToListAsync(cancellationToken);
        return new(classGroup.Id, classGroup.UsesCustomSubjectOffering, subjects);
    }

    public async Task<ClassSubjectOfferingResult> SetAsync(Guid classGroupId, SetClassSubjectsRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var classGroup = await database.ClassGroups.SingleOrDefaultAsync(x => x.Id == classGroupId && x.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Class was not found.");
        var ids = request.SubjectIds.Distinct().ToArray();
        var subjects = await database.Subjects.Where(x => x.TenantId == tenantId && x.IsActive && ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (subjects.Count != ids.Length) throw new InvalidOperationException("One or more subjects were not found for this school.");
        var existing = await database.ClassSubjects.Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId).ToListAsync(cancellationToken);
        var requested = ids.ToHashSet();
        var removed = existing.Where(x => !requested.Contains(x.SubjectId)).ToList();
        if (removed.Count > 0)
        {
            foreach (var offering in removed)
                offering.Deactivate();
        }

        var existingSubjectIds = existing.Select(x => x.SubjectId).ToHashSet();
        foreach (var subjectId in ids.Where(subjectId => !existingSubjectIds.Contains(subjectId)))
        {
            database.ClassSubjects.Add(new ClassSubject(tenantId, classGroupId, subjectId));
        }
        foreach (var offering in existing.Where(x => requested.Contains(x.SubjectId)))
            offering.Activate();
        var removedIds = removed.Select(x => x.Id).ToArray();
        if (removedIds.Length > 0)
        {
            var activeGroups = await database.ParallelSubjectGroups
                .Include(x => x.Members)
                .Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.IsActive && x.Members.Any(member => removedIds.Contains(member.ClassSubjectId)))
                .ToListAsync(cancellationToken);
            foreach (var group in activeGroups)
                group.Deactivate();
        }
        classGroup.SetSubjectOfferingMode(true);
        await database.SaveChangesAsync(cancellationToken);
        return await GetAsync(classGroupId, cancellationToken);
    }

    public async Task<ClassSubjectOfferingResult> ResetAsync(Guid classGroupId, Guid academicSessionId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var classGroup = await database.ClassGroups.SingleOrDefaultAsync(x => x.Id == classGroupId && x.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Class was not found.");
        if (!await database.AcademicSessions.AnyAsync(x => x.Id == academicSessionId && x.TenantId == tenantId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Academic session was not found.");

        var groups = await database.ParallelSubjectGroups
            .Include(x => x.Members)
            // ClassSubject offerings are class-wide, so retiring them must
            // retire every active parallel configuration for this class. The
            // requested session still scopes the reset API and validates the
            // caller's intended session; parallel groups themselves are
            // session-specific and cannot remain active against inactive
            // class offerings in another session.
            .Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var group in groups)
            group.Deactivate();

        var existing = await database.ClassSubjects.Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId).ToListAsync(cancellationToken);
        foreach (var offering in existing)
            offering.Deactivate();
        classGroup.SetSubjectOfferingMode(true);
        await database.SaveChangesAsync(cancellationToken);
        return await GetAsync(classGroupId, cancellationToken);
    }
}
