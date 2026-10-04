using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Api.Endpoints;

public static class PlatformAdminEndpoints
{
    public static void MapPlatformAdminEndpoints(WebApplication app)
    {
        app.MapPost("/api/auth/platform-login", async (PlatformLoginRequest request, IPlatformAuthenticationService authentication, CancellationToken ct) =>
        {
            var result = await authentication.LoginAsync(request, ct);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        }).RequireRateLimiting("auth");

        var group = app.MapGroup("/api/platform-admin").RequireAuthorization();
        group.MapGet("/dashboard", async (SchoolPlatformDbContext db, ICurrentUserContext user, IPlatformAdminService service, CancellationToken ct) =>
            await ExecuteIfAuthorized(db, user, () => service.GetDashboardAsync(ct)));
        group.MapGet("/schools", async (SchoolPlatformDbContext db, ICurrentUserContext user, IPlatformAdminService service, CancellationToken ct) =>
            await ExecuteIfAuthorized(db, user, () => service.GetSchoolsAsync(ct)));
        group.MapGet("/schools/{tenantId:guid}", async (Guid tenantId, SchoolPlatformDbContext db, ICurrentUserContext user, IPlatformAdminService service, CancellationToken ct) =>
            await ExecuteIfAuthorized(db, user, () => service.GetSchoolAsync(tenantId, ct)));
        group.MapPost("/schools", async (CreatePlatformSchoolRequest request, SchoolPlatformDbContext db, ICurrentUserContext user, IPlatformAdminService service, CancellationToken ct) =>
            await ExecuteIfAuthorized(db, user, () => service.CreateSchoolAsync(request, ct), true));
        group.MapPost("/schools/{tenantId:guid}/status", async (Guid tenantId, SetPlatformSchoolStatusRequest request, SchoolPlatformDbContext db, ICurrentUserContext user, IPlatformAdminService service, CancellationToken ct) =>
            await ExecuteIfAuthorized(db, user, async () => { await service.SetSchoolActiveAsync(tenantId, request.IsActive, ct); return new { success = true }; }, true));
        group.MapGet("/schools/{tenantId:guid}/branding", async (Guid tenantId, SchoolPlatformDbContext db, ICurrentUserContext user, IPlatformAdminService service, CancellationToken ct) => await ExecuteIfAuthorized(db, user, () => service.GetBrandingAsync(tenantId, ct)));
        group.MapPut("/schools/{tenantId:guid}/branding", async (Guid tenantId, UpdateTenantBrandingRequest request, SchoolPlatformDbContext db, ICurrentUserContext user, IPlatformAdminService service, CancellationToken ct) => await ExecuteIfAuthorized(db, user, () => service.UpdateBrandingAsync(tenantId, request, ct)));
        group.MapPost("/school-import/website", async (WebsiteImportRequest request, SchoolPlatformDbContext db, ICurrentUserContext user, IWebsiteImportService scanner, CancellationToken ct) => await ExecuteIfAuthorized(db, user, () => scanner.ScanAsync(request.Url, ct)));

        app.MapGet("/api/public/schools/{slug}/branding", async (string slug, SchoolPlatformDbContext db, IPlatformAdminService service, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug.Trim().ToLowerInvariant() && x.IsActive, ct);
            return tenant is null ? Results.NotFound() : Results.Ok(await service.GetBrandingAsync(tenant.Id, ct));
        });

        app.MapPost("/api/platform/bootstrap-super-admin", async (HttpRequest http, BootstrapPlatformAdminRequest request, IConfiguration configuration, IPlatformAdminService service, CancellationToken ct) =>
        {
            var configured = configuration["PLATFORM_BOOTSTRAP_SECRET"];
            if (string.IsNullOrWhiteSpace(configured) || !SecretsMatch(http.Headers["X-Platform-Bootstrap-Secret"].ToString(), configured))
                return Results.Unauthorized();
            await service.PromoteSuperAdminAsync(request.Email, ct);
            return Results.Ok(new { success = true });
        });
    }

    private static async Task<IResult> ExecuteIfAuthorized<T>(SchoolPlatformDbContext db, ICurrentUserContext user, Func<Task<T>> action, bool created = false)
    {
        if (!await IsAuthorized(db, user)) return Results.Forbid();
        try { var result = await action(); return created ? Results.Created("", result) : Results.Ok(result); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    private static async Task<bool> IsAuthorized(SchoolPlatformDbContext db, ICurrentUserContext user)
        => user.IsAuthenticated && user.IsPlatformSuperAdmin && await db.PlatformRoleAssignments.AnyAsync(x => x.UserId == user.UserId && x.Role == PlatformRoles.SuperAdmin && x.IsActive);

    private static bool SecretsMatch(string supplied, string configured)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(configured));
}

public sealed record SetPlatformSchoolStatusRequest(bool IsActive);
public sealed record BootstrapPlatformAdminRequest(string Email);
public sealed record WebsiteImportRequest(string Url);
