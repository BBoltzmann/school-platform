using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolPlatform.Application.Admissions;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Dashboard;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Admissions;
using SchoolPlatform.Domain.Audit;
using SchoolPlatform.Domain.Fees;
using SchoolPlatform.Domain.Students;
using SchoolPlatform.Domain.Staff;
using SchoolPlatform.Domain.Tenancy;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

// All records below are fixtures in isolated disposable test databases, never production seeds.
public sealed class DashboardTests
{
    private static async Task LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AuthenticationFactory.AdminEmail, AuthenticationFactory.OldPassword, "antioch-college"));
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    private static async Task<DashboardResult> DashboardAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/dashboard");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DashboardResult>())!;
    }

    private static async Task EditAsync(AuthenticationFactory factory, Func<SchoolPlatformDbContext, Guid, Task> edit)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenantId = await db.Tenants.Where(x => x.Slug == "antioch-college").Select(x => x.Id).SingleAsync();
        await edit(db, tenantId);
        await db.SaveChangesAsync();
    }

    private static DateOnly Today(AuthenticationFactory factory) => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    [Fact]
    public async Task NewSchoolReturnsActualEmptyStateAndRequiresAuthentication()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/dashboard")).StatusCode);
        await LoginAsync(client);
        var result = await DashboardAsync(client);
        Assert.Equal("antioch-college", result.Tenant.Slug);
        Assert.Equal("Antioch Royal College", result.Tenant.Name);
        Assert.Equal(factory.Clock.GetUtcNow(), result.GeneratedAtUtc);
        Assert.Equal(0, result.Metrics.TotalStudents);
        Assert.Equal(0, result.Metrics.TotalStaff);
        Assert.Equal(0, result.Metrics.PendingApplications);
        Assert.Equal(new DashboardFeesResult(0m, "NGN", "allTime"), result.Metrics.FeesCollected);
        Assert.Null(result.Academic.CurrentSession);
        Assert.Null(result.Academic.CurrentTerm);
        Assert.False(result.Academic.AdmissionsEnabled);
        Assert.NotNull(result.Academic.AdmissionsBlockedReason);
        Assert.Equal(new DashboardActionResult("NO_CURRENT_SESSION", "high", null), Assert.Single(result.Actions));
        Assert.Empty(result.RecentActivity);
        Assert.Equal("notImplemented", result.UpcomingEvents.Availability);
        Assert.False(result.UpcomingEvents.CanCreate);
        Assert.Empty(result.UpcomingEvents.Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CurrentTermRequiresExactlyOneActiveDateMatch(int matches)
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var today = Today(factory);
        Guid sessionId = default;
        await EditAsync(factory, (db, tenantId) =>
        {
            var session = new AcademicSession(tenantId, "Current", today.AddDays(-90), today.AddDays(90), true);
            sessionId = session.Id;
            db.Add(session);
            for (var i = 0; i < matches; i++)
                db.Add(new AcademicTerm(tenantId, session.Id, $"Matching {i}", today, today.AddDays(10), i + 1));
            db.Add(new AcademicTerm(tenantId, session.Id, "Past", today.AddDays(-30), today.AddDays(-10), 5));
            var inactive = new AcademicTerm(tenantId, session.Id, "Inactive", today.AddDays(-10), today.AddDays(10), 6);
            db.Add(inactive);
            db.Entry(inactive).Property(x => x.IsActive).CurrentValue = false;
            var otherSession = new AcademicSession(tenantId, "Other", today.AddDays(-90), today.AddDays(90));
            db.Add(otherSession);
            db.Add(new AcademicTerm(tenantId, otherSession.Id, "Other session term", today.AddDays(-10), today.AddDays(10), 1));
            return Task.CompletedTask;
        });
        await LoginAsync(client);
        var result = await DashboardAsync(client);
        Assert.Equal(sessionId, result.Academic.CurrentSession!.Id);
        Assert.True(result.Academic.AdmissionsEnabled);
        Assert.Null(result.Academic.AdmissionsBlockedReason);
        Assert.DoesNotContain(result.Actions, x => x.Code == "NO_CURRENT_SESSION");
        Assert.Equal(matches == 1, result.Academic.CurrentTerm is not null);
        Assert.Equal(matches != 1, result.Actions.Any(x => x.Code == "NO_CURRENT_TERM"));
        Assert.Equal(matches == 1 ? "ACADEMICS_NOT_CONFIGURED" : "NO_CURRENT_TERM", result.Actions.First().Code);
        Assert.DoesNotContain(result.Actions, x => x.Code == "TIMETABLE_SETUP_REQUIRED");
        if (matches == 1)
        {
            factory.Clock.Advance(TimeSpan.FromDays(10));
            Assert.NotNull((await DashboardAsync(client)).Academic.CurrentTerm); // inclusive end date
            factory.Clock.Advance(TimeSpan.FromDays(1));
            Assert.Null((await DashboardAsync(client)).Academic.CurrentTerm);
        }
    }

    private static Student Student(Guid tenantId, string number) =>
        new(tenantId, number, "Test", null, "Student", new DateOnly(2010, 1, 1), "Female",
            new DateOnly(2025, 1, 1), null, null);

    private static AdmissionApplication Application(Guid tenantId, Guid sessionId, Guid levelId, string number) =>
        new(tenantId, number, "Test", null, "Applicant", new DateOnly(2010, 1, 1), "Female",
            null, null, "Christian", null, null, "Guardian", "Address", "Teacher", "123456789",
            null, sessionId, levelId);

    private static void AddRecords(SchoolPlatformDbContext db, Guid tenantId, DateOnly today, int multiplier)
    {
        var session = new AcademicSession(tenantId, "Current", today.AddDays(-90), today.AddDays(90), true);
        var term = new AcademicTerm(tenantId, session.Id, "Current term", today.AddDays(-10), today.AddDays(10), 1);
        var level = new AcademicLevel(tenantId, "Level", "Primary", 1);
        db.AddRange(session, term, level);
        for (var i = 0; i < multiplier; i++)
        {
            var student = Student(tenantId, $"S{i}");
            student.SetStatus("Inactive");
            var staff = new StaffMember(tenantId, $"T{i}", "Test", null, "Staff", "Female", null, null,
                "123456789", null, today, "Teacher", null, "Full-time", true);
            staff.SetStatus("Inactive");
            db.AddRange(student, staff);
            foreach (var status in new[] { "Submitted", "Under Review", "Waitlisted", "Approved", "Rejected", "Inactive" })
            {
                var application = Application(tenantId, session.Id, level.Id, $"A{i}-{status}");
                db.Add(application);
                // Include every persisted status independently of review workflow side effects.
                db.Entry(application).Property(x => x.Status).CurrentValue = status == "Inactive" ? "Submitted" : status;
                db.Entry(application).Property(x => x.IsActive).CurrentValue = status != "Inactive";
            }
            var valid = new FeePayment(tenantId, student.Id, session.Id, term.Id, 125.75m, "Cash", $"R{i}", null, null);
            var reversed = new FeePayment(tenantId, student.Id, session.Id, term.Id, 500m, "Cash", $"X{i}", null, null);
            reversed.Reverse("Test reversal");
            var item = new FeeItem(tenantId, $"Tuition {i}", null, null);
            var charge = new StudentFeeCharge(tenantId, student.Id, session.Id, term.Id, item.Id, null, null, "Tuition", 300m);
            charge.ApplyPayment(125.75m);
            var secondCharge = new StudentFeeCharge(tenantId, student.Id, session.Id, term.Id, item.Id, null, null, "Other", 20m);
            db.AddRange(valid, reversed, item, charge, secondCharge);
        }
    }

    [Fact]
    public async Task RealRecordsDriveCountsReversalsActionsAndTenantIsolation()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var today = Today(factory);
        Guid otherId = default;
        await EditAsync(factory, (db, tenantId) =>
        {
            AddRecords(db, tenantId, today, 1);
            var other = new Tenant("Other school", "other-school");
            otherId = other.Id;
            db.Add(other);
            AddRecords(db, other.Id, today, 3);
            db.Add(new AuditLog(other.Id, null, "Other tenant activity", "Student"));
            return Task.CompletedTask;
        });
        await LoginAsync(client);
        // A client-supplied query parameter cannot switch the authenticated tenant.
        var response = await client.GetAsync($"/api/dashboard?tenantId={otherId}");
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<DashboardResult>())!;
        Assert.Equal(1, result.Metrics.TotalStudents);
        Assert.Equal(1, result.Metrics.TotalStaff);
        Assert.Equal(2, result.Metrics.PendingApplications);
        Assert.Equal(125.75m, result.Metrics.FeesCollected.Amount);
        Assert.Empty(result.RecentActivity);
        Assert.Equal(new[] { "ACADEMICS_NOT_CONFIGURED", "PENDING_APPLICATIONS", "OUTSTANDING_FEES" },
            result.Actions.Select(x => x.Code));
        Assert.Equal(2, result.Actions.Single(x => x.Code == "PENDING_APPLICATIONS").Count);
        Assert.Equal(1, result.Actions.Single(x => x.Code == "OUTSTANDING_FEES").Count);

        await EditAsync(factory, async (db, tenantId) =>
        {
            var session = await db.AcademicSessions.SingleAsync(x => x.TenantId == tenantId);
            session.RemoveCurrentStatus();
        });
        result = await DashboardAsync(client);
        Assert.Equal(125.75m, result.Metrics.FeesCollected.Amount); // collections do not require a current session
        Assert.Equal("NO_CURRENT_SESSION", result.Actions.First().Code);
        Assert.Null(result.Academic.CurrentTerm); // other tenant's current session cannot leak
    }

    [Fact]
    public async Task ActivityIsLimitedOrderedAndDoesNotExposeSnapshots()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var now = factory.Clock.GetUtcNow().UtcDateTime;
        await EditAsync(factory, (db, tenantId) =>
        {
            for (var i = 0; i < 12; i++)
            {
                var log = new AuditLog(tenantId, null, $"Action {i}", "Student", null,
                    "{\"private\":\"old\"}", "{\"private\":\"new\"}");
                db.Add(log);
                db.Entry(log).Property(x => x.CreatedAtUtc).CurrentValue = now.AddMinutes(i);
            }
            return Task.CompletedTask;
        });
        await LoginAsync(client);
        var result = await DashboardAsync(client);
        Assert.Equal(10, result.RecentActivity.Count);
        Assert.Equal("Action 11", result.RecentActivity.First().Action);
        Assert.Equal("Action 2", result.RecentActivity.Last().Action);
        var json = await client.GetStringAsync("/api/dashboard");
        Assert.DoesNotContain("ValuesJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private", json);
    }

    [Fact]
    public async Task TimetableReadinessAppearsOnlyAfterAcademicSetup()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        await EditAsync(factory, async (db, tenantId) =>
        {
            AddRecords(db, tenantId, Today(factory), 1);
            await db.SaveChangesAsync();
            var level = await db.AcademicLevels.SingleAsync(x => x.TenantId == tenantId);
            var campus = await db.Campuses.SingleAsync(x => x.TenantId == tenantId);
            db.Add(new ClassGroup(tenantId, campus.Id, level.Id, "Class"));
        });
        await LoginAsync(client);
        var result = await DashboardAsync(client);
        Assert.Equal(new[] { "PENDING_APPLICATIONS", "OUTSTANDING_FEES", "TIMETABLE_SETUP_REQUIRED" },
            result.Actions.Select(x => x.Code));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdmissionCreationRequiresCurrentSession(bool current)
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        Guid sessionId = default, levelId = default;
        await EditAsync(factory, (db, tenantId) =>
        {
            var today = Today(factory);
            var session = new AcademicSession(tenantId, "Requested", today.AddDays(-90), today.AddDays(90), current);
            var level = new AcademicLevel(tenantId, "Level", "Primary", 1);
            sessionId = session.Id;
            levelId = level.Id;
            db.AddRange(session, level);
            if (!current)
                db.Add(new AcademicSession(tenantId, "Actual current", today.AddDays(-90), today.AddDays(90), true));
            return Task.CompletedTask;
        });
        await LoginAsync(client);
        var request = new CreateAdmissionApplicationRequest("Test", null, "Applicant", new DateOnly(2010, 1, 1),
            "Female", null, null, "Christian", null, null, "Guardian", "Address", "Teacher", "123456789",
            null, sessionId, levelId);
        var response = await client.PostAsJsonAsync("/api/admissions", request);
        Assert.Equal(current, response.IsSuccessStatusCode);
        if (!current) Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await EditAsync(factory, async (db, _) => Assert.Equal(current ? 1 : 0, await db.AdmissionApplications.CountAsync()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DashboardRejectsNonAdministratorOrMissingSourcePermission(bool removeRole)
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        await EditAsync(factory, async (db, _) =>
        {
            if (removeRole)
                db.MembershipRoles.RemoveRange(await db.MembershipRoles.ToListAsync());
            else
            {
                var permission = await db.Permissions.SingleAsync(x => x.Code == "staff.read");
                db.RolePermissions.RemoveRange(await db.RolePermissions.Where(x => x.PermissionId == permission.Id).ToListAsync());
            }
        });
        await LoginAsync(client);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/dashboard")).StatusCode);
    }
}
