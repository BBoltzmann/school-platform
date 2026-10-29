using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Domain.Staff;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

public sealed class TeacherInvitationTests
{
    [Fact]
    public async Task FreshInvitationActivatesImmediatelyAndCannotBeReused()
    {
        using var factory = new AuthenticationFactory();
        var client = await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenantId = await database.Tenants.Where(x => x.Slug == "antioch-college").Select(x => x.Id).SingleAsync();
        var staff = new StaffMember(tenantId, "T/001", "Ada", null, "Teacher", "Female", null, "ada.teacher@example.test", "08000000000", null, new DateOnly(2026, 1, 1), "Teacher", null, "Full-time", true);
        database.StaffMembers.Add(staff);
        await database.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "antioch-college"));
        var session = await login.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session!.AccessToken);
        var generated = await client.PostAsJsonAsync($"/api/staff/{staff.Id}/teacher-invite", new { });
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
        var invite = await generated.Content.ReadFromJsonAsync<TeacherInviteResponse>();
        var token = ExtractToken(invite!.InviteUrl);
        var preview = await client.GetFromJsonAsync<TeacherPreview>($"/api/auth/teacher-invite/{token.ToLowerInvariant()}");
        Assert.Equal("ada.teacher@example.test", preview!.Email);

        var activation = await client.PostAsJsonAsync("/api/auth/teacher-invite/activate", new { token, email = "ada.teacher@example.test", password = "Teacher-password-123" });
        Assert.Equal(HttpStatusCode.OK, activation.StatusCode);
        var second = await client.PostAsJsonAsync("/api/auth/teacher-invite/activate", new { token, email = "ada.teacher@example.test", password = "Another-password-456" });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var teacherLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ada.teacher@example.test", "Teacher-password-123", "antioch-college"));
        Assert.Equal(HttpStatusCode.OK, teacherLogin.StatusCode);
    }

    [Fact]
    public async Task ResetReplacesOlderInvitationWithoutDuplicatingTeacher()
    {
        using var factory = new AuthenticationFactory();
        var client = await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenantId = await database.Tenants.Where(x => x.Slug == "antioch-college").Select(x => x.Id).SingleAsync();
        var staff = new StaffMember(tenantId, "T/002", "Bola", null, "Teacher", "Female", null, "bola.teacher@example.test", "08000000000", null, new DateOnly(2026, 1, 1), "Teacher", null, "Full-time", true);
        database.StaffMembers.Add(staff);
        await database.SaveChangesAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "antioch-college"));
        var session = await login.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session!.AccessToken);
        var oldResponse = await client.PostAsJsonAsync($"/api/staff/{staff.Id}/teacher-invite", new { });
        var old = await oldResponse.Content.ReadFromJsonAsync<TeacherInviteResponse>();
        var resetResponse = await client.PostAsJsonAsync($"/api/staff/{staff.Id}/teacher-invite/reset", new { });
        var reset = await resetResponse.Content.ReadFromJsonAsync<TeacherInviteResponse>();
        Assert.NotEqual(old!.InviteUrl, reset!.InviteUrl);
        var oldToken = ExtractToken(old.InviteUrl);
        var newToken = ExtractToken(reset.InviteUrl);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/auth/teacher-invite/{oldToken}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/auth/teacher-invite/{newToken}")).StatusCode);
    }

    private static string ExtractToken(string inviteUrl) => Uri.UnescapeDataString(new Uri(inviteUrl).Query["?token=".Length..]);

    private sealed record TeacherInviteResponse(string InviteUrl, DateTime ExpiresAtUtc);
    private sealed record TeacherPreview(string TenantName, string StaffName, string? Email, DateTime ExpiresAtUtc);
}
