using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Infrastructure.Persistence;
using SchoolPlatform.Infrastructure.Platform;

namespace SchoolPlatform.IntegrationTests;

public sealed class PlatformAdminTests
{
    [PostgresTimetableFact]
    public async Task PlatformEndpointsRequirePlatformIdentityAndExposeCrossSchoolDataOnlyToIt()
    {
        using var factory = new AuthenticationFactory();
        var client = await factory.InitializeAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/platform-admin/schools")).StatusCode);

        var tenantLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "antioch-college"));
        var tenantSession = await tenantLogin.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantSession!.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/platform-admin/schools")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/platform-admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/platform-admin/schools/00000000-0000-0000-0000-000000000001")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/platform-admin/schools", new CreatePlatformSchoolRequest("Denied", "denied", "Campus", "A", "Admin", "denied@example.com"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/platform-admin/schools/00000000-0000-0000-0000-000000000001/status", new { isActive = false })).StatusCode);

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatform.Infrastructure.Persistence.SchoolPlatformDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IPlatformAdminService>();
        var admin = await database.Users.SingleAsync(x => x.Email == AuthenticationFactory.AdminEmail);
        await service.PromoteSuperAdminAsync(admin.Email);

        var platformLogin = await client.PostAsJsonAsync("/api/auth/platform-login", new PlatformLoginRequest(
            AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword));
        var platformSession = await platformLogin.Content.ReadFromJsonAsync<PlatformLoginResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", platformSession!.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/platform-admin/schools")).StatusCode);
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(platformSession.AccessToken).Claims.Select(x => x.Type).ToHashSet();
        Assert.DoesNotContain("tenant_id", claims);
        Assert.DoesNotContain("tenant_slug", claims);
        Assert.DoesNotContain("membership_id", claims);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/academics/setup")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/platform/bootstrap-super-admin", new { email = AuthenticationFactory.AdminEmail })).StatusCode);
    }

    [PostgresTimetableFact]
    public async Task PlatformAdminCanProvisionAndSuspendSchoolWithoutDuplicatingGlobalUser()
    {
        using var factory = new AuthenticationFactory();
        var client = await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IPlatformAdminService>();
        var admin = await database.Users.SingleAsync(x => x.Email == AuthenticationFactory.AdminEmail);
        await service.PromoteSuperAdminAsync(admin.Email);

        var created = await service.CreateSchoolAsync(new CreatePlatformSchoolRequest(
            "Second School", "second-school", "Second campus", "School", "Admin", admin.Email));
        Assert.Equal("second-school", created.School.Summary.Slug);
        Assert.False(created.AdministratorActivationPending);
        Assert.Equal(2, await database.TenantMemberships.CountAsync(x => x.UserId == admin.Id));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "antioch-college"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "second-school"))).StatusCode);

        await service.SetSchoolActiveAsync(created.School.Summary.TenantId, false);
        Assert.False(await database.Tenants.Where(x => x.Id == created.School.Summary.TenantId).Select(x => x.IsActive).SingleAsync());
        await service.SetSchoolActiveAsync(created.School.Summary.TenantId, true);
        Assert.True(await database.Tenants.Where(x => x.Id == created.School.Summary.TenantId).Select(x => x.IsActive).SingleAsync());

        var dashboard = await service.GetDashboardAsync();
        Assert.True(dashboard.TotalSchools >= 2);
    }

    [PostgresTimetableFact]
    public async Task ExistingTenantTokenStopsWorkingAfterSuspensionAndWorksAfterReactivation()
    {
        using var factory = new AuthenticationFactory();
        var client = await factory.InitializeAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "antioch-college"));
        var session = await login.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenantId = await database.Tenants.Where(x => x.Slug == "antioch-college").Select(x => x.Id).SingleAsync();
        var service = scope.ServiceProvider.GetRequiredService<IPlatformAdminService>();
        await service.SetSchoolActiveAsync(tenantId, false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/academics/setup")).StatusCode);

        await service.SetSchoolActiveAsync(tenantId, true);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/academics/setup")).StatusCode);
    }

    [PostgresTimetableFact]
    public async Task PlatformAdminCanPersistTenantBrandingWithoutChangingTenantBootstrapData()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IPlatformAdminService>();
        var tenant = await database.Tenants.SingleAsync(x => x.Slug == "antioch-college");
        var result = await service.UpdateBrandingAsync(tenant.Id, new UpdateTenantBrandingRequest(
            "https://school.example", "office@school.example", "+123", "Campus Road", "Learn and serve", "Our mission", "Our vision", "About the school", null, null, "#112233", "#445566", "#778899"));
        Assert.Equal("#112233", result.PrimaryColor);
        Assert.Equal("Learn and serve", (await service.GetBrandingAsync(tenant.Id)).Motto);
        Assert.Equal("Antioch Royal College", await database.Tenants.Where(x => x.Id == tenant.Id).Select(x => x.Name).SingleAsync());
    }

    [PostgresTimetableFact]
    public async Task EmptySchoolCanBeDeletedButPopulatedSchoolCannot()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IPlatformAdminService>();
        var admin = await database.Users.SingleAsync(x => x.Email == AuthenticationFactory.AdminEmail);
        await service.PromoteSuperAdminAsync(admin.Email);

        var created = await service.CreateSchoolAsync(new CreatePlatformSchoolRequest(
            "Disposable Demo", "disposable-demo", "Demo campus", "Demo", "Admin", admin.Email));
        var eligibility = await service.GetDeletionEligibilityAsync(created.School.Summary.TenantId);
        Assert.True(eligibility.CanDeletePermanently);
        await service.DeleteEmptySchoolAsync(created.School.Summary.TenantId);
        Assert.False(await database.Tenants.AnyAsync(x => x.Id == created.School.Summary.TenantId));

        var populatedTenant = await database.Tenants.SingleAsync(x => x.Slug == "antioch-college");
        var populatedEligibility = await service.GetDeletionEligibilityAsync(populatedTenant.Id);
        Assert.False(populatedEligibility.CanDeletePermanently);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteEmptySchoolAsync(populatedTenant.Id));
        Assert.True(await database.Tenants.AnyAsync(x => x.Id == populatedTenant.Id));
    }

    [PostgresTimetableFact]
    public async Task NewSchoolAdministratorReceivesOneTimeSetupLink()
    {
        using var factory = new AuthenticationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IPlatformAdminService>();
        var admin = await database.Users.SingleAsync(x => x.Email == AuthenticationFactory.AdminEmail);
        await service.PromoteSuperAdminAsync(admin.Email);

        var created = await service.CreateSchoolAsync(new CreatePlatformSchoolRequest(
            "Invited Demo", "invited-demo", "Demo campus", "New", "Administrator", "new-admin@example.com"));

        Assert.True(created.AdministratorActivationPending);
        Assert.Contains("/reset-password?token=", created.AdministratorSetupLink);
        var invited = await database.Users.SingleAsync(x => x.Email == "new-admin@example.com");
        Assert.True(await database.TenantMemberships.AnyAsync(x => x.TenantId == created.School.Summary.TenantId && x.UserId == invited.Id));
        Assert.Single(await database.PasswordResetTokens.Where(x => x.TenantId == created.School.Summary.TenantId && x.UserId == invited.Id).ToListAsync());
    }
}
