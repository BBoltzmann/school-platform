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
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var tenantId =
                    tenantContext.TenantId;

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
                            Array.Empty<object>()
                    });
                }

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
                            x.AcademicLevel.IsActive)
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
                                x.AcademicLevel.Name
                        })
                        .ToListAsync(
                            cancellationToken);

                var subjects =
                    await database.Subjects
                        .AsNoTracking()
                        .Where(x =>
                            x.TenantId == tenantId &&
                            x.IsActive)
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
                    subjects
                });
            })
            .RequireAuthorization();
    }
}
