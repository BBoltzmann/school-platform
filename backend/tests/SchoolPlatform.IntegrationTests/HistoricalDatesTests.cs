using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolPlatform.Application.Academics;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Staff;
using SchoolPlatform.Application.Students;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

public sealed class HistoricalDatesTests
{
    private static async Task LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "antioch-college"));
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<LoginResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
    }

    private static async Task<(Guid SessionId, Guid LevelId, Guid ClassId)> AddAcademicSetupAsync(AuthenticationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenant = await db.Tenants.SingleAsync(x => x.Slug == "antioch-college");
        var campus = await db.Campuses.SingleAsync(x => x.TenantId == tenant.Id);
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        var session = new AcademicSession(tenant.Id, "2026/2027", today.AddDays(-30), today.AddDays(300), true);
        var level = new AcademicLevel(tenant.Id, "JSS", "Secondary", 1);
        var @class = new ClassGroup(tenant.Id, campus.Id, level.Id, "JSS 3");
        db.AddRange(session, level, @class);
        await db.SaveChangesAsync();
        return (session.Id, level.Id, @class.Id);
    }

    [Fact]
    public async Task StudentHistoricalAdmissionAndPlacementDatesArePersisted()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var setup = await AddAcademicSetupAsync(factory);
        await LoginAsync(client);

        var create = new CreateStudentRequest("ARC/2020/001", "Historical", null, "Student",
            new DateOnly(2012, 1, 1), "Female", new DateOnly(2020, 9, 14), null, null,
            setup.SessionId, setup.LevelId, setup.ClassId);
        var response = await client.PostAsJsonAsync("/api/students", create);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<StudentListItemResult>())!;
        Assert.Equal(create.AdmissionDate, result.AdmissionDate);

        var placement = await client.PatchAsJsonAsync($"/api/students/{result.Id}/placement",
            new UpdateStudentPlacementRequest(setup.LevelId, setup.ClassId, new DateOnly(2019, 1, 14)));
        placement.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        Assert.Equal(new DateOnly(2020, 9, 14), await db.Students.Where(x => x.Id == result.Id).Select(x => x.AdmissionDate).SingleAsync());
        Assert.Equal(new DateOnly(2019, 1, 14), await db.StudentEnrollments.Where(x => x.StudentId == result.Id).Select(x => x.EnrollmentDate).SingleAsync());
    }

    [Fact]
    public async Task StudentFutureDateOfBirthStillFails()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var setup = await AddAcademicSetupAsync(factory);
        await LoginAsync(client);
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        var response = await client.PostAsJsonAsync("/api/students", new CreateStudentRequest("ARC/FUTURE", "Future", null, "Student",
            today.AddDays(1), "Female", new DateOnly(2020, 9, 14), null, null, setup.SessionId, setup.LevelId, setup.ClassId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StaffHistoricalEmploymentDateIsPersistedAndCanBeEdited()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        await LoginAsync(client);
        var create = new CreateStaffRequest("STAFF/2018/001", "Historical", null, "Staff", "Female", new DateOnly(1980, 1, 1),
            null, "08000000000", null, new DateOnly(2018, 1, 8), "Teacher", "Science", "Full-Time", true);
        var response = await client.PostAsJsonAsync("/api/staff", create);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<StaffMemberResult>())!;
        Assert.Equal(create.EmploymentDate, result.EmploymentDate);

        var updateRequest = new UpdateStaffRequest("Updated", create.MiddleName, create.LastName, create.Gender,
            create.DateOfBirth, create.Email, create.Phone, create.Address, new DateOnly(2016, 9, 1),
            create.JobTitle, create.Department, create.EmploymentType, create.IsTeachingStaff, "Active");
        var update = await client.PatchAsJsonAsync($"/api/staff/{result.Id}", updateRequest);
        update.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        Assert.Equal(new DateOnly(2016, 9, 1), await db.StaffMembers.Where(x => x.Id == result.Id).Select(x => x.EmploymentDate).SingleAsync());
    }

    [Fact]
    public async Task StaffFutureDateOfBirthStillFails()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        await LoginAsync(client);
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        var response = await client.PostAsJsonAsync("/api/staff", new CreateStaffRequest("STAFF/FUTURE", "Future", null, "Staff", "Female",
            today.AddDays(1), null, "08000000000", null, new DateOnly(2018, 1, 8), "Teacher", null, "Full-Time", true));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
