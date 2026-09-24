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
            ? await database.ClassSubjects.AsNoTracking().Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.Subject.IsActive).OrderBy(x => x.Subject.Name).Select(x => new ClassSubjectResult(x.Id, x.SubjectId, x.Subject.Name, x.Subject.Code)).ToListAsync(cancellationToken)
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
        var existingAssigned = await database.TeachingAssignments.AsNoTracking().Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId && x.IsActive).Select(x => x.SubjectId).Distinct().ToListAsync(cancellationToken);
        if (existingAssigned.Any(x => !ids.Contains(x))) throw new InvalidOperationException("A subject with an existing teaching assignment cannot be removed.");
        // Update the offering set in place.  Replacing every row on each save
        // made rapid edits fragile and also broke the FK from configured
        // parallel groups.  Keeping unchanged ClassSubject rows preserves the
        // authoritative IDs used by requirements and groups.
        var existing = await database.ClassSubjects.Where(x => x.TenantId == tenantId && x.ClassGroupId == classGroupId).ToListAsync(cancellationToken);
        var requested = ids.ToHashSet();
        var removed = existing.Where(x => !requested.Contains(x.SubjectId)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(x => x.Id).ToArray();
            var grouped = await database.ParallelSubjectGroupMembers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && removedIds.Contains(x.ClassSubjectId) && x.ParallelSubjectGroup.IsActive)
                .Select(x => x.ClassSubject.Subject.Name)
                .Distinct()
                .ToListAsync(cancellationToken);
            if (grouped.Count > 0)
            {
                throw new InvalidOperationException($"Remove the parallel group containing {string.Join(", ", grouped)} before removing those subjects from this class.");
            }
            database.ClassSubjects.RemoveRange(removed);
        }

        var existingSubjectIds = existing.Select(x => x.SubjectId).ToHashSet();
        foreach (var subjectId in ids.Where(subjectId => !existingSubjectIds.Contains(subjectId)))
        {
            database.ClassSubjects.Add(new ClassSubject(tenantId, classGroupId, subjectId));
        }
        classGroup.SetSubjectOfferingMode(true);
        await database.SaveChangesAsync(cancellationToken);
        return await GetAsync(classGroupId, cancellationToken);
    }
}
