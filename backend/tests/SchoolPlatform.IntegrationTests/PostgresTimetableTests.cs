using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SchoolPlatform.Application.Timetabling;
using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Tenancy;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.IntegrationTests;

public sealed class PostgresTimetableTests
{
    [PostgresTimetableFact]
    public async Task MigrationPreservesRowsAcrossAllThreeFeatureMigrations()
    {
        await using var fixture = new PostgresTimetableFixture();
        await fixture.InitializeAsync(baseline: true, slashName: true);
        await using var db = fixture.Open();
        var timetableId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO generated_timetables ("Id", "TenantId", "AcademicSessionId", "AcademicTermId", "GeneratedAtUtc", "CreatedAtUtc")
            VALUES ({timetableId}, {fixture.TenantId}, {fixture.SessionId}, {fixture.TermId}, {DateTime.UtcNow}, {DateTime.UtcNow});
            INSERT INTO generated_timetable_entries ("Id", "TenantId", "GeneratedTimetableId", "ClassGroupId", "SubjectId", "StaffMemberId", "DayOfWeek", "PeriodNumber", "StartTime", "EndTime", "CreatedAtUtc")
            VALUES ({entryId}, {fixture.TenantId}, {timetableId}, {fixture.ClassId}, {fixture.SubjectIds[0]}, {fixture.TeacherIds[0]}, 1, 1, TIME '08:00', TIME '09:00', {DateTime.UtcNow});
            """);
        string[] tables = ["tenants", "campuses", "academic_sessions", "academic_terms", "academic_levels", "class_groups", "subjects", "class_subjects", "class_subject_requirements", "teaching_assignments", "staff_members", "students", "timetable_settings", "timetable_days", "generated_timetables", "generated_timetable_entries"];
        async Task<string> Snapshot(string table)
        {
            await db.Database.OpenConnectionAsync();
            await using var command = db.Database.GetDbConnection().CreateCommand();
            var excluded = table switch
            {
                "generated_timetables" => "ARRAY['VersionNumber','IsActive','ActivatedAtUtc','SupersededAtUtc']",
                "generated_timetable_entries" => "ARRAY['ParallelSubjectGroupId','ParallelOccurrenceId']",
                _ => "ARRAY[]::text[]"
            };
            command.CommandText = $"SELECT COALESCE(jsonb_agg(to_jsonb(t) - {excluded} ORDER BY t.\"Id\")::text, '[]') FROM {table} t";
            return (string)(await command.ExecuteScalarAsync())!;
        }
        var before = new Dictionary<string, string>();
        foreach (var table in tables) before[table] = await Snapshot(table);
        foreach (var migration in PostgresTimetableFixture.FeatureMigrations)
        {
            await db.GetService<IMigrator>().MigrateAsync(migration);
            Assert.Equal(migration, (await db.Database.GetAppliedMigrationsAsync()).Last());
            foreach (var table in tables) Assert.Equal(before[table], await Snapshot(table));
        }
        var timetable = await db.GeneratedTimetables.SingleAsync();
        Assert.Equal(timetableId, timetable.Id);
        Assert.True(timetable.IsActive);
        Assert.Equal(1, timetable.VersionNumber);
        Assert.Equal(timetable.GeneratedAtUtc, timetable.ActivatedAtUtc);
        var entry = await db.GeneratedTimetableEntries.SingleAsync();
        Assert.Equal(entryId, entry.Id);
        Assert.Null(entry.ParallelOccurrenceId);
        Assert.Empty(await db.ParallelSubjectGroups.ToListAsync());
        Assert.Contains(await db.Subjects.ToListAsync(), x => x.Name == "History/Geography");
    }

    [PostgresTimetableFact] public Task TwoMemberParallelGeneration() => AssertParallel(2);
    [PostgresTimetableFact] public Task ThreeMemberParallelGeneration() => AssertParallel(3);

    [PostgresTimetableFact]
    public async Task RegenerationKeepsOneEntryPerParallelMember()
    {
        await AssertRegenerationKeepsOneEntryPerParallelMember(2);
        await AssertRegenerationKeepsOneEntryPerParallelMember(3);
    }

    private static async Task AssertRegenerationKeepsOneEntryPerParallelMember(int members)
    {
        await using var fixture = new PostgresTimetableFixture();
        await fixture.InitializeAsync(members);
        await fixture.GenerateAsync();
        var active = await fixture.GenerateAsync();

        Assert.Equal(2, active.VersionNumber);
        Assert.All(active.Entries.Where(x => x.ClassGroupId == fixture.ClassId).GroupBy(x => x.ParallelOccurrenceId), occurrence =>
        {
            var identities = occurrence.Select(x => (x.SubjectId, x.StaffMemberId)).ToList();
            Assert.Equal(members, identities.Count);
            Assert.Equal(members, identities.Distinct().Count());
        });

        await using var db = fixture.Open();
        var history = await fixture.Service(db).GetHistoryAsync(fixture.TermId);
        Assert.Equal(2, history.Count);
        Assert.Single(history.Where(x => x.IsActive));
    }

    [PostgresTimetableFact]
    public async Task ActiveTimetableReadIsTenantScoped()
    {
        await using var fixture = new PostgresTimetableFixture();
        await fixture.InitializeAsync();
        await using var db = fixture.Open();
        var foreignTenant = new Tenant("Other fixture", "other-fixture");
        var foreignSession = new AcademicSession(foreignTenant.Id, "Other session", new(2026, 9, 1), new(2027, 7, 1), true);
        var foreignTerm = new AcademicTerm(foreignTenant.Id, foreignSession.Id, "Other term", new(2026, 9, 1), new(2026, 12, 20), 1);
        db.AddRange(foreignTenant, foreignSession, foreignTerm);
        await db.SaveChangesAsync();
        var foreignTimetable = new GeneratedTimetable(foreignTenant.Id, foreignSession.Id, foreignTerm.Id);
        db.Add(foreignTimetable);
        await db.SaveChangesAsync();
        var result = await fixture.Service(db).GetAsync(foreignTerm.Id);
        Assert.Null(result);
    }

    [PostgresTimetableFact]
    public async Task DoublePeriodPreferenceGroupsThreePeriods()
    {
        await using var fixture = new PostgresTimetableFixture();
        await fixture.InitializeAsync(subjectPeriods: 3);
        var result = await fixture.GenerateAsync();
        var entries = result.Entries.Where(x => x.ClassGroupId == fixture.ClassId && x.SubjectId == fixture.SubjectIds[0]).OrderBy(x => x.DayOfWeek).ThenBy(x => x.PeriodNumber).ToList();
        Assert.Equal(3, entries.Count);
        Assert.Contains(entries.Zip(entries.Skip(1)), pair => pair.First.DayOfWeek == pair.Second.DayOfWeek && pair.First.EndTime == pair.Second.StartTime);
    }

    [PostgresTimetableFact]
    public async Task DoublePeriodPreferenceGroupsFourPeriods()
    {
        await using var fixture = new PostgresTimetableFixture();
        await fixture.InitializeAsync(subjectPeriods: 4);
        var result = await fixture.GenerateAsync();
        var entries = result.Entries.Where(x => x.ClassGroupId == fixture.ClassId && x.SubjectId == fixture.SubjectIds[0]).OrderBy(x => x.DayOfWeek).ThenBy(x => x.PeriodNumber).ToList();
        Assert.Equal(4, entries.Count);
        Assert.Equal(2, entries.Zip(entries.Skip(1)).Count(pair => pair.First.DayOfWeek == pair.Second.DayOfWeek && pair.First.EndTime == pair.Second.StartTime));
    }

    [PostgresTimetableFact]
    public async Task DoublePeriodPreferenceGroupsFiveAndSixPeriods()
    {
        foreach (var periods in new[] { 5, 6 })
        {
            await using var fixture = new PostgresTimetableFixture();
            await fixture.InitializeAsync(subjectPeriods: periods);
            var result = await fixture.GenerateAsync();
            var entries = result.Entries.Where(x => x.ClassGroupId == fixture.ClassId && x.SubjectId == fixture.SubjectIds[0]).OrderBy(x => x.DayOfWeek).ThenBy(x => x.PeriodNumber).ToList();
            Assert.Equal(periods, entries.Count);
            Assert.True(entries.Zip(entries.Skip(1)).Count(pair => pair.First.DayOfWeek == pair.Second.DayOfWeek && pair.First.EndTime == pair.Second.StartTime) >= periods / 2);
        }
    }

    [PostgresTimetableFact]
    public async Task BreakBoundaryIsNotTreatedAsConsecutive()
    {
        await using var fixture = new PostgresTimetableFixture();
        await fixture.InitializeAsync(subjectPeriods: 3, withBreak: true);
        var result = await fixture.GenerateAsync();
        var entries = result.Entries.Where(x => x.ClassGroupId == fixture.ClassId && x.SubjectId == fixture.SubjectIds[0]).ToList();
        Assert.DoesNotContain(entries.Zip(entries.Skip(1)), pair =>
            pair.First.DayOfWeek == pair.Second.DayOfWeek &&
            pair.First.EndTime == new TimeOnly(9, 20) &&
            pair.Second.StartTime == new TimeOnly(10, 0));
    }
    private static async Task AssertParallel(int members)
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(members);
        var result = await f.GenerateAsync();
        Assert.Equal(members * 2 + 2, result.EntryCount);
        var groups = result.Entries.Where(x => x.ClassGroupId == f.ClassId).GroupBy(x => x.ParallelOccurrenceId).ToList();
        Assert.Equal(2, groups.Count);
        Assert.All(groups, occurrence =>
        {
            Assert.NotNull(occurrence.Key);
            Assert.Equal(members, occurrence.Count());
            Assert.Equal(members, occurrence.Select(x => x.SubjectId).Distinct().Count());
            Assert.Equal(members, occurrence.Select(x => x.StaffMemberId).Distinct().Count());
            Assert.Single(occurrence.Select(x => (x.DayOfWeek, x.PeriodNumber, x.StartTime, x.EndTime)).Distinct());
            Assert.All(occurrence, x => Assert.Equal(f.GroupId, x.ParallelSubjectGroupId));
        });
        AssertNoTeacherCollision(result);
        await AssertActive(f, result.Id, 1);
    }

    [PostgresTimetableFact]
    public async Task UnavailableParallelTeacherRejectsGeneration()
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(2);
        await f.MakeTeacherUnavailableAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => f.GenerateAsync());
        Assert.Contains("simultaneously", error.Message);
        await using var db = f.Open();
        Assert.Empty(await db.GeneratedTimetables.ToListAsync());
        Assert.Empty(await db.GeneratedTimetableEntries.ToListAsync());
    }

    [PostgresTimetableFact]
    public async Task CrossClassTeacherCollisionIsAvoided()
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(2);
        var result = await f.GenerateAsync();
        Assert.Equal(4, result.Entries.Count(x => x.StaffMemberId == f.TeacherIds[0]));
        Assert.Equal(2, result.Entries.Where(x => x.StaffMemberId == f.TeacherIds[0]).Select(x => x.ClassGroupId).Distinct().Count());
        AssertNoTeacherCollision(result);
    }

    [PostgresTimetableFact] public Task ZeroGroupLegacyBehavior() => AssertLegacy(false);
    [PostgresTimetableFact] public Task SlashNamedLegacySubjectRemainsOneSubject() => AssertLegacy(true);
    private static async Task AssertLegacy(bool slash)
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(slashName: slash);
        var result = await f.GenerateAsync();
        Assert.Equal(6, result.EntryCount);
        Assert.All(result.Entries, x => { Assert.Null(x.ParallelOccurrenceId); Assert.Null(x.ParallelSubjectGroupId); });
        Assert.All(result.Entries.GroupBy(x => (x.ClassGroupId, x.DayOfWeek, x.PeriodNumber)), x => Assert.Single(x));
        if (slash) Assert.Equal(4, result.Entries.Count(x => x.SubjectName == "History/Geography" && x.SubjectId == f.SubjectIds[0]));
        AssertNoTeacherCollision(result);
    }

    [PostgresTimetableFact]
    public async Task SuccessfulVersionedRegenerationPreservesHistory()
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync();
        var first = await f.GenerateAsync();
        var second = await f.GenerateAsync();
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, second.VersionNumber);
        await AssertActive(f, second.Id, 2);
        await AssertStoredEntries(f, first);
    }

    [PostgresTimetableFact] public Task FailedFullRegenerationRollsBackAfterDeactivation() => AssertRollback(false);
    [PostgresTimetableFact] public Task FailedClassRegenerationRollsBackAfterDeactivation() => AssertRollback(true);
    private static async Task AssertRollback(bool classOnly)
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(2);
        var first = await f.GenerateAsync();
        var failure = new FailEntryInsert();
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => f.GenerateAsync(classOnly ? f.ClassId : null, failure));
        Assert.Equal("Injected failure after version deactivation, before entry insertion.", Assert.IsType<InvalidOperationException>(error.InnerException).Message);
        Assert.True(failure.Triggered);
        await AssertActive(f, first.Id, 1);
        await AssertStoredEntries(f, first);
        var next = await f.GenerateAsync(classOnly ? f.ClassId : null);
        Assert.Equal(2, next.VersionNumber);
    }

    [PostgresTimetableFact]
    public async Task FailedPlanningPreservesActiveVersion()
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(2);
        var first = await f.GenerateAsync();
        await f.MakeTeacherUnavailableAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.GenerateAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.GenerateAsync(f.ClassId));
        await AssertActive(f, first.Id, 1);
        await AssertStoredEntries(f, first);
    }

    [PostgresTimetableFact]
    public async Task ResetPreservesHistoryAndNextVersionNumber()
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync();
        var first = await f.GenerateAsync();
        await using (var db = f.Open())
        {
            await f.Service(db).ResetAsync(f.TermId);
            Assert.Null(await f.Service(db).GetAsync(f.TermId));
            var history = await f.Service(db).GetHistoryAsync(f.TermId);
            Assert.False(Assert.Single(history).IsActive);
            Assert.NotNull(history.Single().SupersededAtUtc);
        }
        await AssertStoredEntries(f, first);
        var next = await f.GenerateAsync();
        Assert.Equal(2, next.VersionNumber);
        await AssertActive(f, next.Id, 2);
    }

    [PostgresTimetableFact] public Task ClassRegenerationPreservesOtherClasses() => AssertClassRegeneration(0);
    [PostgresTimetableFact] public Task ParallelClassRegenerationPreservesOtherClasses() => AssertClassRegeneration(3);
    private static async Task AssertClassRegeneration(int members)
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(members);
        var first = await f.GenerateAsync();
        var next = await f.GenerateAsync(f.ClassId);
        Assert.Equal(Signatures(first, f.OtherClassId), Signatures(next, f.OtherClassId));
        Assert.Equal(first.EntryCount, next.EntryCount);
        if (members > 0)
        {
            Assert.Equal(2, next.Entries.Where(x => x.ClassGroupId == f.ClassId).Select(x => x.ParallelOccurrenceId).Distinct().Count());
            Assert.All(next.Entries.Where(x => x.ClassGroupId == f.ClassId), x => Assert.NotNull(x.ParallelOccurrenceId));
        }
        AssertNoTeacherCollision(next);
        await AssertStoredEntries(f, first);
        await AssertActive(f, next.Id, 2);
    }

    [PostgresTimetableFact] public Task ConcurrentFullRegenerations() => AssertConcurrent(false);
    [PostgresTimetableFact] public Task ConcurrentFullAndClassRegenerations() => AssertConcurrent(true);
    private static async Task AssertConcurrent(bool classOnly)
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(2);
        var first = await f.GenerateAsync();
        var barrier = new StartTogether();
        var results = await Task.WhenAll(f.GenerateAsync(null, barrier), f.GenerateAsync(classOnly ? f.ClassId : null, barrier));
        Assert.Equal(new[] { 2, 3 }, results.Select(x => x.VersionNumber).Order().ToArray());
        Assert.Equal(2, results.Select(x => x.Id).Distinct().Count());
        Assert.All(results, AssertNoTeacherCollision);
        await AssertActive(f, results.Single(x => x.VersionNumber == 3).Id, 3);
        await AssertStoredEntries(f, first);
        foreach (var result in results) await AssertStoredEntries(f, result);
    }

    [PostgresTimetableFact]
    public async Task DatabaseRejectsSecondActiveVersion()
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync();
        var first = await f.GenerateAsync();
        await using var db = f.Open();
        var duplicate = new GeneratedTimetable(f.TenantId, f.SessionId, f.TermId);
        duplicate.SetVersion(2);
        db.Add(duplicate);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
        await AssertActive(f, first.Id, 1);
    }

    [PostgresTimetableFact]
    public async Task DatabaseRejectsDuplicateParallelMemberAndUnpairedOccurrence()
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync(2);
        var result = await f.GenerateAsync();
        var original = result.Entries.First(x => x.ClassGroupId == f.ClassId);
        await using (var db = f.Open())
        {
            db.Add(new GeneratedTimetableEntry(f.TenantId, result.Id, f.ClassId, original.SubjectId,
                Guid.NewGuid(), original.DayOfWeek, original.PeriodNumber, original.StartTime, original.EndTime,
                original.ParallelSubjectGroupId, original.ParallelOccurrenceId));
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            var postgres = Assert.IsType<PostgresException>(error.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal("IX_generated_timetable_entries_parallel_occurrence_slot", postgres.ConstraintName);
        }
        await using (var db = f.Open())
        {
            db.Add(new GeneratedTimetableEntry(f.TenantId, result.Id, f.ClassId, original.SubjectId,
                original.StaffMemberId, DayOfWeek.Tuesday, 1, new(8, 0), new(9, 0), f.GroupId));
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.CheckViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
        }
        await AssertStoredEntries(f, result);
    }

    [PostgresTimetableFact]
    public async Task MigratedIndexesMatchTheEfModel()
    {
        await using var f = new PostgresTimetableFixture();
        await f.InitializeAsync();
        await using var db = f.Open();
        var indexes = await db.Database.SqlQueryRaw<string>("SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = 'public'").ToListAsync();
        string[] tables = ["parallel_subject_groups", "parallel_subject_group_members", "generated_timetables", "generated_timetable_entries"];
        foreach (var entity in db.Model.GetEntityTypes().Where(x => tables.Contains(x.GetTableName())))
            foreach (var index in entity.GetIndexes())
                Assert.Contains(index.GetDatabaseName(), indexes);
    }

    private static string[] Signatures(GeneratedTimetableResult result, Guid classId) => result.Entries.Where(x => x.ClassGroupId == classId)
        .Select(x => $"{x.SubjectId}|{x.StaffMemberId}|{x.DayOfWeek}|{x.PeriodNumber}|{x.StartTime}|{x.EndTime}|{x.ParallelSubjectGroupId}|{x.ParallelOccurrenceId}").Order().ToArray();

    private static void AssertNoTeacherCollision(GeneratedTimetableResult result) =>
        Assert.All(result.Entries.GroupBy(x => (x.StaffMemberId, x.DayOfWeek, x.PeriodNumber)), x => Assert.Single(x));

    private static async Task AssertActive(PostgresTimetableFixture f, Guid id, int versions)
    {
        await using var db = f.Open();
        var all = await db.GeneratedTimetables.ToListAsync();
        Assert.Equal(versions, all.Count);
        Assert.Equal(id, Assert.Single(all.Where(x => x.IsActive)).Id);
        Assert.All(all.Where(x => !x.IsActive), x => Assert.NotNull(x.SupersededAtUtc));
    }

    private static async Task AssertStoredEntries(PostgresTimetableFixture f, GeneratedTimetableResult expected)
    {
        await using var db = f.Open();
        var rows = await db.GeneratedTimetableEntries.Where(x => x.GeneratedTimetableId == expected.Id).ToListAsync();
        Assert.Equal(expected.Entries.Select(x => x.Id).Order(), rows.Select(x => x.Id).Order());
        foreach (var entry in expected.Entries)
        {
            var row = rows.Single(x => x.Id == entry.Id);
            Assert.Equal((entry.ClassGroupId, entry.SubjectId, entry.StaffMemberId, entry.DayOfWeek, entry.PeriodNumber, entry.StartTime, entry.EndTime, entry.ParallelSubjectGroupId, entry.ParallelOccurrenceId),
                (row.ClassGroupId, row.SubjectId, row.StaffMemberId, row.DayOfWeek, row.PeriodNumber, row.StartTime, row.EndTime, row.ParallelSubjectGroupId, row.ParallelOccurrenceId));
        }
    }

    private sealed class FailEntryInsert : DbCommandInterceptor
    {
        public bool Triggered { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO generated_timetable_entries", StringComparison.Ordinal))
            {
                Triggered = true;
                throw new InvalidOperationException("Injected failure after version deactivation, before entry insertion.");
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    // Both independent connections begin regeneration transactions together.
    private sealed class StartTogether : DbTransactionInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref arrivals) <= 2)
            {
                if (arrivals == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }
}
