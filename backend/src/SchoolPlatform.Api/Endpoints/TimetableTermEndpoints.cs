using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Api.Endpoints;

public static class TimetableTermEndpoints
{
    public static void MapTimetableTermEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/timetable/terms",
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

                var currentSessionId =
                    await database.AcademicSessions
                        .AsNoTracking()
                        .Where(x =>
                            x.TenantId == tenantId &&
                            x.IsCurrent &&
                            x.IsActive)
                        .Select(x => (Guid?)x.Id)
                        .FirstOrDefaultAsync(
                            cancellationToken);

                if (currentSessionId is null)
                {
                    return Results.Ok(
                        Array.Empty<object>());
                }

                var terms =
                    await database.AcademicTerms
                        .AsNoTracking()
                        .Where(x =>
                            x.TenantId == tenantId &&
                            x.AcademicSessionId ==
                                currentSessionId.Value &&
                            x.IsActive)
                        .OrderBy(x => x.Name)
                        .Select(x => new
                        {
                            x.Id,
                            x.Name
                        })
                        .ToListAsync(
                            cancellationToken);

                return Results.Ok(terms);
            })
            .RequireAuthorization();
    }
}
