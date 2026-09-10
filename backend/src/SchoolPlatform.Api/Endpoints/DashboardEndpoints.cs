using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Dashboard;

namespace SchoolPlatform.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard", async (
            IDashboardService dashboard,
            ICurrentUserContext currentUser,
            CancellationToken cancellationToken) =>
        {
            // This consolidated administrator response must not bypass source permissions.
            if (!currentUser.Roles.Contains("Administrator", StringComparer.OrdinalIgnoreCase) ||
                !currentUser.HasPermission("students.read") ||
                !currentUser.HasPermission("staff.read") ||
                !currentUser.HasPermission("admissions.read") ||
                !currentUser.HasPermission("academics.configure"))
                return Results.Forbid();

            return Results.Ok(await dashboard.GetAsync(cancellationToken));
        }).RequireAuthorization();
    }
}
