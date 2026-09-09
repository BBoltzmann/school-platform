using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

public sealed class AuthenticationRecoveryTests
{
    private static readonly ForgotPasswordRequest Known = new(AuthenticationFactory.AdminEmail, "antioch-college");
    private static CreateSchoolRequest Signup(string slug = "new-school", string email = "new-admin@example.com") =>
        new("New School", slug, "Primary campus", email, "New", "Admin", AuthenticationFactory.NewPassword);

    [Fact]
    public async Task UnknownAndKnownAccountsHaveIdenticalResponsesAndDurableRequests()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var known = await client.PostAsJsonAsync("/api/auth/forgot-password", Known);
        var unknown = await client.PostAsJsonAsync("/api/auth/forgot-password", Known with { Email = "unknown@example.com" });
        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.Contains("If an account exists, a password reset link has been sent.", await known.Content.ReadAsStringAsync());
        Assert.Empty(factory.Email.Messages);
        await factory.DeliverNextAsync();
        await factory.DeliverNextAsync();
        Assert.Single(factory.Email.Messages);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        Assert.Empty(await db.PasswordRecoveryJobs.ToListAsync());
        var token = await db.PasswordResetTokens.SingleAsync();
        var raw = factory.Email.LatestToken;
        Assert.Equal(64, raw.Length);
        Assert.NotEqual(raw, token.TokenHash);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))), token.TokenHash);
        Assert.Equal(factory.Clock.GetUtcNow().UtcDateTime.AddMinutes(30), token.ExpiresAtUtc);
        Assert.Null(token.UsedAtUtc);
    }

    [Fact]
    public async Task RecoveryRequiresActiveMembershipInTheRequestedSchool()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        await client.PostAsJsonAsync("/api/auth/forgot-password", Known with { TenantSlug = "different-school" });
        await factory.DeliverNextAsync();
        Assert.Empty(factory.Email.Messages);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public async Task InvalidTokenIsRejected(string token)
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, AuthenticationFactory.NewPassword));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
    }

    [Fact]
    public async Task ExpiredTokenIsRejected()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        await client.PostAsJsonAsync("/api/auth/forgot-password", Known);
        await factory.DeliverNextAsync();
        factory.Clock.Advance(TimeSpan.FromMinutes(30));
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(factory.Email.LatestToken, AuthenticationFactory.NewPassword));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
    }

    [Fact]
    public async Task SuccessfulResetConsumesAllTokensAndOnlyNewPasswordCanLogin()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        await client.PostAsJsonAsync("/api/auth/forgot-password", Known);
        await factory.DeliverNextAsync();
        var oldSession = JsonDocument.Parse(await (await Login(client, AuthenticationFactory.OldPassword)).Content.ReadAsStringAsync())
            .RootElement.GetProperty("accessToken").GetString();
        var firstToken = factory.Email.LatestToken;
        factory.Clock.Advance(TimeSpan.FromMinutes(2));
        await client.PostAsJsonAsync("/api/auth/forgot-password", Known);
        await factory.DeliverNextAsync();
        Assert.Equal(2, factory.Email.Messages.Count);
        var secondToken = factory.Email.LatestToken;
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(firstToken, AuthenticationFactory.NewPassword));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("antioch-college", await response.Content.ReadAsStringAsync());
        foreach (var token in new[] { firstToken, secondToken })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/reset-password",
                new ResetPasswordRequest(token, AuthenticationFactory.OldPassword))).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldSession);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tenant/context")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
        var newLogin = await Login(client, AuthenticationFactory.NewPassword);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        var newSession = JsonDocument.Parse(await newLogin.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newSession);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/tenant/context")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        Assert.All(await db.PasswordResetTokens.ToListAsync(), token => Assert.NotNull(token.UsedAtUtc));
        Assert.Single(await db.Tenants.ToListAsync());
        Assert.Single(await db.TenantMemberships.ToListAsync());
    }

    [Fact]
    public async Task WeakPasswordDoesNotConsumeTheToken()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        await client.PostAsJsonAsync("/api/auth/forgot-password", Known);
        await factory.DeliverNextAsync();
        var token = factory.Email.LatestToken;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, "short"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, AuthenticationFactory.NewPassword))).StatusCode);
    }

    [Fact]
    public async Task DeliveryFailureDoesNotLeakToBrowserAndCanRetry()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        factory.Email.Fail = true;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/forgot-password", Known)).StatusCode);
        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.DeliverNextAsync());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
            Assert.Single(await db.PasswordRecoveryJobs.ToListAsync());
            Assert.NotNull((await db.PasswordResetTokens.SingleAsync()).UsedAtUtc);
        }
        factory.Email.Fail = false;
        factory.Clock.Advance(TimeSpan.FromMinutes(3));
        await factory.DeliverNextAsync();
        Assert.Single(factory.Email.Messages);
    }

    [Fact]
    public async Task DisabledSignupAndDevelopmentEndpointsAreUnavailableInProduction()
    {
        using var factory = new AuthenticationFactory(false);
        using var client = await factory.InitializeAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/auth/signup", Signup())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/platform/bootstrap-school", Signup())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/platform/set-initial-password", new { email = AuthenticationFactory.AdminEmail, password = "anything" })).StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>().Tenants.ToListAsync());
    }

    [Fact]
    public async Task EnabledSignupCreatesSchoolCampusAndAdministratorWithAllBootstrapPermissions()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var response = await client.PostAsJsonAsync("/api/auth/signup", Signup(" NEW-SCHOOL ", " NEW-ADMIN@example.com "));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await db.Tenants.SingleAsync(x => x.Slug == "new-school");
        var user = await db.Users.SingleAsync(x => x.Email == "new-admin@example.com");
        var member = await db.TenantMemberships.SingleAsync(x => x.TenantId == tenant.Id);
        var role = await db.Roles.SingleAsync(x => x.TenantId == tenant.Id);
        Assert.Equal(user.Id, member.UserId);
        Assert.Equal("Administrator", role.Name);
        Assert.Equal("Primary campus", (await db.Campuses.SingleAsync(x => x.TenantId == tenant.Id)).Name);
        Assert.NotEqual(AuthenticationFactory.NewPassword, user.PasswordHash);
        var assignment = await db.MembershipRoles.SingleAsync(x => x.TenantId == tenant.Id);
        Assert.Equal(member.Id, assignment.MembershipId);
        Assert.Equal(role.Id, assignment.RoleId);
        Assert.Equal(await db.Permissions.CountAsync(), await db.RolePermissions.CountAsync(x => x.TenantId == tenant.Id));
        Assert.True(await db.RolePermissions.CountAsync(x => x.TenantId == tenant.Id) >= 20);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("new-admin@example.com", AuthenticationFactory.NewPassword, "new-school"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("new-admin@example.com", AuthenticationFactory.NewPassword, "antioch-college"))).StatusCode);
    }

    [Fact]
    public async Task DuplicateOrReservedSlugCannotBeJoinedOrOverwritten()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/signup", Signup("antioch-college"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/signup", Signup())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/signup", Signup(email: "another@example.com"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        Assert.Equal(2, await db.Tenants.CountAsync());
        Assert.Equal(2, await db.Users.CountAsync());
    }

    [Fact]
    public async Task ExistingGlobalEmailIsNotDisclosedJoinedOrOverwritten()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var existing = await client.PostAsJsonAsync("/api/auth/signup", Signup("attempt-school", AuthenticationFactory.AdminEmail));
        var created = await client.PostAsJsonAsync("/api/auth/signup", Signup());
        Assert.Equal(created.StatusCode, existing.StatusCode);
        Assert.Equal(await created.Content.ReadAsStringAsync(), await existing.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        Assert.False(await db.Tenants.AnyAsync(x => x.Slug == "attempt-school"));
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
    }

    [Fact]
    public async Task LoginAndForgotPasswordAreRateLimited()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        for (var attempt = 0; attempt < 3; attempt++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/forgot-password", Known)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/forgot-password", Known)).StatusCode);
        for (var attempt = 0; attempt < 10; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "wrong-password")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client, "wrong-password")).StatusCode);
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthenticationFactory.AdminEmail, password, "antioch-college"));
}
