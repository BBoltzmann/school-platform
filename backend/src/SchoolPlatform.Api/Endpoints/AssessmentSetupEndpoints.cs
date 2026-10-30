using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Api.Endpoints;

public static class AssessmentSetupEndpoints
{
    public static void MapAssessmentSetupEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/assessments/setup",
            async (
                SchoolPlatformDbContext database,
                ITenantContext tenantContext,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.read") &&
                    !currentUser.HasPermission("teacher.subjects.read"))
                {
                    return Results.Forbid();
                }

                var tenantId =
                    tenantContext.TenantId;

                var isAdministrator = currentUser.HasPermission("students.read");

                var currentSession =
                    await database.AcademicSessions
                        .AsNoTracking()
                        .Where(x =>
                            x.TenantId == tenantId &&
                            x.IsCurrent &&
                            x.IsActive)
                        .Select(x => new
                        {
                            x.Id,
                            x.Name
                        })
                        .SingleOrDefaultAsync(
                            cancellationToken);

                if (currentSession is null)
                {
                    return Results.Ok(new
                    {
                        currentSession =
                            (object?)null,
                        terms =
                            Array.Empty<object>(),
                        classes =
                            Array.Empty<object>(),
                        subjects =
                            Array.Empty<object>(),
                        assignments =
                            Array.Empty<object>()
                    });
                }

                // Only expose the current teacher's active class/subject assignments.
                // Administrators retain unrestricted school assessment setup.
                var teacherAssignments = isAdministrator
                    ? new List<(Guid ClassGroupId, Guid SubjectId)>()
                    : (await (
                        from staff in database.StaffMembers.AsNoTracking()
                        join assignment in database.TeachingAssignments.AsNoTracking()
                            on staff.Id equals assignment.StaffMemberId
                        where staff.TenantId == tenantId &&
                              staff.UserId == currentUser.UserId &&
                              staff.IsActive && staff.IsTeachingStaff &&
                              assignment.TenantId == tenantId &&
                              assignment.AcademicSessionId == currentSession.Id &&
                              assignment.IsActive &&
                              assignment.ClassGroup.IsActive &&
                              assignment.ClassGroup.AcademicLevel.IsActive &&
                              (!assignment.ClassGroup.UsesCustomSubjectOffering ||
                               database.ClassSubjects.Any(offering =>
                                   offering.TenantId == tenantId &&
                                   offering.ClassGroupId == assignment.ClassGroupId &&
                                   offering.SubjectId == assignment.SubjectId &&
                                   offering.IsActive)) &&
                              assignment.Subject.IsActive
                        select new { assignment.ClassGroupId, assignment.SubjectId }
                    ).Distinct().ToListAsync(cancellationToken))
                        .Select(x => (x.ClassGroupId, x.SubjectId))
                        .ToList();

                var allowedClassIds = teacherAssignments
                    .Select(x => x.ClassGroupId).Distinct().ToArray();
                var terms =
                    await database.AcademicTerms
                        .AsNoTracking()
                        .Where(x =>
                            x.TenantId == tenantId &&
                            x.AcademicSessionId ==
                                currentSession.Id &&
                            x.IsActive)
                        .OrderBy(x => x.Name)
                        .Select(x => new
                        {
                            x.Id,
                            x.Name
                        })
                        .ToListAsync(
                            cancellationToken);

                var classes =
                    await database.ClassGroups
                        .AsNoTracking()
                        .Where(x =>
                            x.TenantId == tenantId &&
                            x.IsActive &&
                            x.AcademicLevel.IsActive &&
                            (isAdministrator || allowedClassIds.Contains(x.Id)))
                        .OrderBy(x =>
                            x.AcademicLevel.Name)
                        .ThenBy(x => x.Name)
                        .Select(x => new
                        {
                            x.Id,
                            x.Name,
                            AcademicLevelId =
                                x.AcademicLevelId,
                            AcademicLevelName =
                                x.AcademicLevel.Name,
                            x.UsesCustomSubjectOffering
                        })
                        .ToListAsync(
                            cancellationToken);

                var customOfferingPairs = await database.ClassSubjects
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.IsActive && x.Subject.IsActive &&
                                x.ClassGroup.IsActive && x.ClassGroup.AcademicLevel.IsActive &&
                                x.ClassGroup.UsesCustomSubjectOffering &&
                                classes.Select(item => item.Id).Contains(x.ClassGroupId))
                    .Select(x => new { x.ClassGroupId, x.SubjectId })
                    .ToListAsync(cancellationToken);
                var assignmentOfferingPairs = await database.TeachingAssignments
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.AcademicSessionId == currentSession.Id &&
                                x.IsActive && x.Subject.IsActive && x.ClassGroup.IsActive &&
                                x.ClassGroup.AcademicLevel.IsActive && !x.ClassGroup.UsesCustomSubjectOffering &&
                                classes.Select(item => item.Id).Contains(x.ClassGroupId))
                    .Select(x => new { x.ClassGroupId, x.SubjectId })
                    .Distinct()
                    .ToListAsync(cancellationToken);
                var offeredPairs = customOfferingPairs
                    .Concat(assignmentOfferingPairs)
                    .DistinctBy(x => (x.ClassGroupId, x.SubjectId))
                    .ToArray();
                var visiblePairs = isAdministrator
                    ? offeredPairs
                    : teacherAssignments
                        .Where(pair => offeredPairs.Any(offered => offered.ClassGroupId == pair.ClassGroupId && offered.SubjectId == pair.SubjectId))
                        .Distinct()
                        .Select(pair => new { pair.ClassGroupId, pair.SubjectId })
                        .ToArray();
                var visibleSubjectIds = visiblePairs.Select(x => x.SubjectId).Distinct().ToArray();
                var subjects =
                    await database.Subjects
                        .AsNoTracking()
                        .Where(x =>
                            x.TenantId == tenantId &&
                            x.IsActive &&
                            visibleSubjectIds.Contains(x.Id))
                        .OrderBy(x => x.Name)
                        .Select(x => new
                        {
                            x.Id,
                            x.Name
                        })
                        .ToListAsync(
                            cancellationToken);

                return Results.Ok(new
                {
                    currentSession,
                    terms,
                    classes,
                    subjects,
                    assignments = visiblePairs.Select(x => new
                    {
                        x.ClassGroupId,
                        x.SubjectId
                    }).ToArray()
                });
            })
            .RequireAuthorization();
    }
}
