using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

public sealed class DirectPasswordResetTests
{
    private const string Endpoint = "/api/auth/direct-password-reset";
    private static DirectPasswordResetRequest Request(AuthenticationFactory factory) => new(
        AuthenticationFactory.AdminEmail, "antioch-college", factory.DirectRecoveryCode!, AuthenticationFactory.NewPassword);
    private static Task<HttpResponseMessage> Login(HttpClient client, string password) => client.PostAsJsonAsync(
        "/api/auth/login", new LoginRequest(AuthenticationFactory.AdminEmail, password, "antioch-college"));

    [Fact]
    public async Task FeatureIsDisabledByDefaultAndCanBeDisabledAtRuntime()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Endpoint, Request(factory))).StatusCode);
        factory.DirectResetEnabled = true;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Endpoint, Request(factory))).StatusCode);
        factory.DirectResetEnabled = false;
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Endpoint, Request(factory))).StatusCode);
    }

    [Fact]
    public async Task WrongCodeAndUnknownUserHaveIdenticalGenericFailures()
    {
        using var factory = new AuthenticationFactory(directResetEnabled: true);
        using var client = await factory.InitializeAsync();
        var wrong = await client.PostAsJsonAsync(Endpoint, Request(factory) with { RecoveryCode = "incorrect" });
        var unknown = await client.PostAsJsonAsync(Endpoint, Request(factory) with { Email = "unknown@example.com" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        Assert.Equal(await wrong.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
        Assert.Empty(factory.Email.Messages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public async Task MissingOrWeakServerCodeFailsClosed(string? code)
    {
        using var factory = new AuthenticationFactory(directResetEnabled: true) { DirectRecoveryCode = code };
        using var client = await factory.InitializeAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Endpoint, Request(factory))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
    }

    [Fact]
    public async Task EmailAndPasswordWithoutARecoveryCodeCannotResetAnAccount()
    {
        using var factory = new AuthenticationFactory(directResetEnabled: true);
        using var client = await factory.InitializeAsync();
        var response = await client.PostAsJsonAsync(Endpoint, new {
            email = AuthenticationFactory.AdminEmail, tenantSlug = "antioch-college", newPassword = AuthenticationFactory.NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
    }

    [Fact]
    public async Task CorrectCodeChangesPasswordRevokesSessionsAndConsumesExistingResetTokens()
    {
        using var factory = new AuthenticationFactory(directResetEnabled: true);
        using var client = await factory.InitializeAsync();
        var oldLogin = await Login(client, AuthenticationFactory.OldPassword);
        using var oldJson = JsonDocument.Parse(await oldLogin.Content.ReadAsStringAsync());
        var oldJwt = oldJson.RootElement.GetProperty("accessToken").GetString();
        await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(AuthenticationFactory.AdminEmail, "antioch-college"));
        await factory.DeliverNextAsync();
        string? oldStamp;
        using (var scope = factory.Services.CreateScope())
            oldStamp = (await scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>().Users.SingleAsync()).SecurityStamp;
        var emailCount = factory.Email.Messages.Count;
        var response = await client.PostAsJsonAsync(Endpoint, Request(factory) with { Email = " ADMIN@ANTIOCHCOLLEGE.LOCAL ", TenantSlug = " ANTIOCH-COLLEGE " });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("antioch-college", await response.Content.ReadAsStringAsync());
        Assert.Equal(emailCount, factory.Email.Messages.Count);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
        var newLogin = await Login(client, AuthenticationFactory.NewPassword);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldJwt);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tenant/context")).StatusCode);
        using var newJson = JsonDocument.Parse(await newLogin.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newJson.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/tenant/context")).StatusCode);
        using var checkScope = factory.Services.CreateScope();
        var db = checkScope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var user = await db.Users.SingleAsync();
        Assert.NotEqual(oldStamp, user.SecurityStamp);
        Assert.All(await db.PasswordResetTokens.ToListAsync(), token => Assert.NotNull(token.UsedAtUtc));
        Assert.Single(await db.Tenants.ToListAsync());
        Assert.Single(await db.TenantMemberships.ToListAsync());
    }

    [Fact]
    public async Task CorrectCodeCannotResetUserThroughAnotherTenant()
    {
        using var factory = new AuthenticationFactory(directResetEnabled: true);
        using var client = await factory.InitializeAsync();
        var response = await client.PostAsJsonAsync(Endpoint, Request(factory) with { TenantSlug = "different-school" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("membership")]
    [InlineData("tenant")]
    public async Task InactiveUserMembershipOrTenantCannotBeRecovered(string inactive)
    {
        using var factory = new AuthenticationFactory(directResetEnabled: true);
        using var client = await factory.InitializeAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
            if (inactive == "user") await db.Users.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
            else if (inactive == "membership") await db.TenantMemberships.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
            else await db.Tenants.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Endpoint, Request(factory))).StatusCode);
    }

    [Fact]
    public async Task WeakPasswordIsRejectedEvenWithCorrectCode()
    {
        using var factory = new AuthenticationFactory(directResetEnabled: true);
        using var client = await factory.InitializeAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Endpoint, Request(factory) with { NewPassword = "short" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, AuthenticationFactory.OldPassword)).StatusCode);
    }

    [Fact]
    public async Task RecoveryIsLimitedPerAccountAndAcrossAccountNames()
    {
        using var factory = new AuthenticationFactory(directResetEnabled: true);
        using var client = await factory.InitializeAsync();
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Endpoint, Request(factory) with { RecoveryCode = "wrong" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(Endpoint, Request(factory))).StatusCode);
        for (var i = 0; i < 6; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Endpoint, Request(factory) with { Email = $"unknown{i}@example.com", RecoveryCode = "wrong" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(Endpoint, Request(factory) with { Email = "another@example.com" })).StatusCode);
    }
}
