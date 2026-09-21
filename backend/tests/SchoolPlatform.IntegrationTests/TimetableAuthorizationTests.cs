using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Tenancy;
using SchoolPlatform.Domain.Timetabling;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

public sealed class TimetableAuthorizationTests
{
    [Fact]
    public async Task GeneratedTimetableRequiresAuthenticationAndTenantOwnership()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var foreignTermId = await AddForeignTermAsync(factory);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync($"/api/timetable/generated/{foreignTermId}")).StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "antioch-college"));
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/timetable/generated/{foreignTermId}")).StatusCode);
    }

    [Fact]
    public async Task TeacherPortalTimetableEndpointsDoNotAcceptClientTeacherIds()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/me/teacher-portal?teacherId=00000000-0000-0000-0000-000000000001")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/me/classes/00000000-0000-0000-0000-000000000001/timetable?teacherId=00000000-0000-0000-0000-000000000002")).StatusCode);
    }

    private static async Task<Guid> AddForeignTermAsync(AuthenticationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = new Tenant("Other school", "other-school");
        var session = new AcademicSession(tenant.Id, "Other session", new(2026, 9, 1), new(2027, 7, 1), true);
        var term = new AcademicTerm(tenant.Id, session.Id, "Other term", new(2026, 9, 1), new(2026, 12, 20), 1);
        database.AddRange(tenant, session, term, new GeneratedTimetable(tenant.Id, session.Id, term.Id));
        await database.SaveChangesAsync();
        return term.Id;
    }
}
