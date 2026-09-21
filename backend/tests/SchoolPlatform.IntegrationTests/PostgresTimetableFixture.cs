using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Timetabling;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Staff;
using SchoolPlatform.Domain.Students;
using SchoolPlatform.Domain.Tenancy;
using SchoolPlatform.Domain.Timetabling;
using SchoolPlatform.Infrastructure.Persistence;
using SchoolPlatform.Infrastructure.Timetabling;

namespace SchoolPlatform.IntegrationTests;

public sealed class PostgresTimetableFactAttribute : FactAttribute
{
    public PostgresTimetableFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SCHOOL_AUTH_TEST_POSTGRES_CONNECTION")))
            Skip = "Set SCHOOL_AUTH_TEST_POSTGRES_CONNECTION to local PostgreSQL at 127.0.0.1:5433.";
    }
}

// Each test owns a random database. No application database is ever opened.
internal sealed class PostgresTimetableFixture : IAsyncDisposable, ITenantContext
{
    public const string Baseline = "20260915182241_AddTeacherPortalInvitations";
    public static readonly string[] FeatureMigrations =
    [
        "20260921120000_AddParallelSubjectGroupFoundation",
        "20260921150000_AddParallelTimetableOccurrence",
        "20260922100000_AddTimetableVersions"
    ];
    private readonly string databaseName = "school_timetable_test_" + Guid.NewGuid().ToString("N");
    private readonly string adminConnection;
    private readonly string connection;
    public Guid TenantId { get; private set; }
    public string TenantSlug => "timetable-fixture";
    public Guid SessionId { get; private set; }
    public Guid TermId { get; private set; }
    public Guid ClassId { get; private set; }
    public Guid OtherClassId { get; private set; }
    public Guid? GroupId { get; private set; }
    public List<Guid> SubjectIds { get; } = [];
    public List<Guid> TeacherIds { get; } = [];
    private bool created;

    public PostgresTimetableFixture()
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SCHOOL_AUTH_TEST_POSTGRES_CONNECTION"));
        if (builder.Host != "127.0.0.1" || builder.Port != 5433)
            throw new InvalidOperationException("Only local PostgreSQL at 127.0.0.1:5433 is allowed.");
        builder.Pooling = false;
        builder.Database = "postgres";
        adminConnection = builder.ConnectionString;
        builder.Database = databaseName;
        connection = builder.ConnectionString;
    }

    public SchoolPlatformDbContext Open(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<SchoolPlatformDbContext>().UseNpgsql(connection)
            .AddInterceptors(interceptors).Options);

    public async Task InitializeAsync(int members = 0, bool baseline = false, bool slashName = false, int subjectPeriods = 2, bool withBreak = false)
    {
        await using (var admin = new NpgsqlConnection(adminConnection))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await command.ExecuteNonQueryAsync();
            created = true;
        }
        await using var db = Open();
        Assert.True(db.Database.IsNpgsql());
        Assert.Equal(FeatureMigrations, db.Database.GetMigrations().TakeLast(3));
        await db.GetService<IMigrator>().MigrateAsync(baseline ? Baseline : null);
        var tenant = new Tenant("Timetable fixture", TenantSlug);
        TenantId = tenant.Id;
        var campus = new Campus(TenantId, "Fixture campus");
        var session = new AcademicSession(TenantId, "2026/27", new(2026, 9, 1), new(2027, 7, 1), true);
        SessionId = session.Id;
        var term = new AcademicTerm(TenantId, SessionId, "First term", new(2026, 9, 1), new(2026, 12, 20), 1);
        TermId = term.Id;
        var level = new AcademicLevel(TenantId, "Year 1", "Primary", 1);
        var first = new ClassGroup(TenantId, campus.Id, level.Id, "A");
        var second = new ClassGroup(TenantId, campus.Id, level.Id, "B");
        ClassId = first.Id;
        OtherClassId = second.Id;
        var settings = new TimetableSettings(TenantId, SessionId, subjectPeriods >= 3 ? 40 : 60);
        var days = Enum.GetValues<DayOfWeek>().Where(day => day is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
            .Select(day => new TimetableDay(TenantId, settings.Id, day, new(8, 0), new(14, 0))).ToArray();
        db.AddRange(tenant, campus, session, term, level, first, second, settings,
            new Student(TenantId, "STUDENT-1", "Fixture", null, "Student", new(2015, 1, 1), "Female", new(2026, 9, 1), null, null));
        db.AddRange(days);
        if (withBreak)
            db.Add(new TimetableNonTeachingBlock(TenantId, days[0].Id, "Break", new(9, 20), new(10, 0), 1));
        ParallelSubjectGroup? group = members > 0 ? new(TenantId, SessionId, ClassId, "Options") : null;
        GroupId = group?.Id;
        if (group is not null) db.Add(group);
        for (var i = 0; i < Math.Max(2, members); i++)
        {
            var subject = new Subject(TenantId, slashName && i == 0 ? "History/Geography" : $"Subject {i}", $"S{i}", "Core");
            var teacher = new StaffMember(TenantId, $"T{i}", "Teacher", null, $"{i}", "Female", null, null,
                "123", null, new(2026, 1, 1), "Teacher", null, "Full-Time", true);
            SubjectIds.Add(subject.Id);
            TeacherIds.Add(teacher.Id);
            var offering = new ClassSubject(TenantId, ClassId, subject.Id);
            db.AddRange(subject, teacher, offering,
                new ClassSubjectRequirement(TenantId, SessionId, ClassId, subject.Id, subjectPeriods),
                new TeachingAssignment(TenantId, teacher.Id, SessionId, ClassId, subject.Id));
            if (group is not null) db.Add(new ParallelSubjectGroupMember(TenantId, group.Id, offering.Id));
            if (i == 0)
                db.AddRange(new ClassSubject(TenantId, OtherClassId, subject.Id),
                    new ClassSubjectRequirement(TenantId, SessionId, OtherClassId, subject.Id, subjectPeriods),
                    new TeachingAssignment(TenantId, teacher.Id, SessionId, OtherClassId, subject.Id));
        }
        await db.SaveChangesAsync();
    }

    public TimetableGenerationService Service(SchoolPlatformDbContext db) =>
        new(db, this, new TimetableReadinessService(db, this));

    public async Task<GeneratedTimetableResult> GenerateAsync(Guid? classId = null, params IInterceptor[] interceptors)
    {
        await using var db = Open(interceptors);
        return await Service(db).GenerateAsync(new(TermId, classId));
    }

    public async Task MakeTeacherUnavailableAsync()
    {
        await using var db = Open();
        var teacher = await db.StaffMembers.SingleAsync(x => x.Id == TeacherIds[1]);
        teacher.UpdateEmployment(new(2026, 1, 1), "Teacher", null, "Part-Time", true);
        // The readiness check sees Monday, but the scheduler must reject these nonoverlapping hours.
        db.Add(new StaffAvailability(TenantId, teacher.Id, DayOfWeek.Monday, new(16, 0), new(17, 0)));
        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (!created) return;
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }
}
