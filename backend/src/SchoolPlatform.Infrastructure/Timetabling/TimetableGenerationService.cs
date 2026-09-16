using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Timetabling;
using SchoolPlatform.Domain.Timetabling;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Timetabling;

public sealed class TimetableGenerationService
    : ITimetableGenerationService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;
    private readonly ITimetableReadinessService _readiness;

    public TimetableGenerationService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext,
        ITimetableReadinessService readiness)
    {
        _database = database;
        _tenantContext = tenantContext;
        _readiness = readiness;
    }

    public async Task<GeneratedTimetableResult> GenerateAsync(
        GenerateTimetableRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AcademicTermId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Academic term is required.");
        }

        var tenantId =
            _tenantContext.TenantId;

        var readiness =
            await _readiness.GetReadinessAsync(
                cancellationToken);

        if (!readiness.CanGenerate)
        {
            var firstIssue =
                readiness.Issues
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Severity,
                            "Error",
                            StringComparison.OrdinalIgnoreCase));

            throw new InvalidOperationException(
                firstIssue is null
                    ? "The timetable is not ready to generate."
                    : $"The timetable is not ready to generate. {firstIssue.Message}");
        }

        var session =
            await _database.AcademicSessions
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsCurrent &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name
                })
                .FirstOrDefaultAsync(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "A current academic session is required.");

        var termExists =
            await _database.AcademicTerms
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id ==
                            request.AcademicTermId &&
                        x.TenantId ==
                            tenantId &&
                        x.AcademicSessionId ==
                            session.Id &&
                        x.IsActive,
                    cancellationToken);

        if (!termExists)
        {
            throw new InvalidOperationException(
                "The selected academic term was not found in the current academic session.");
        }

        var settings =
            await _database.TimetableSettings
                .AsNoTracking()
                .Include(x => x.Days)
                .ThenInclude(x =>
                    x.NonTeachingBlocks)
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.AcademicSessionId == session.Id &&
                        x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Timetable structure has not been configured.");

        var slots =
            BuildSlots(settings);

        if (slots.Count == 0)
        {
            throw new InvalidOperationException(
                "The timetable structure does not contain any teaching periods.");
        }

        var requirements =
            await _database.ClassSubjectRequirements
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.IsActive &&
                    (!x.ClassGroup.UsesCustomSubjectOffering ||
                     x.ClassGroup.ClassSubjects.Any(cs => cs.SubjectId == x.SubjectId)))
                .Select(x => new RequirementRecord(
                    x.ClassGroupId,
                    x.ClassGroup.Name,
                    x.ClassGroup.AcademicLevel.Name,
                    x.SubjectId,
                    x.Subject.Name,
                    x.PeriodsPerWeek))
                .ToListAsync(
                    cancellationToken);

        var assignments =
            await _database.TeachingAssignments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.IsActive &&
                    x.StaffMember.IsActive &&
                    x.StaffMember.IsTeachingStaff)
                .Select(x => new AssignmentRecord(
                    x.ClassGroupId,
                    x.SubjectId,
                    x.StaffMemberId,
                    x.StaffMember.MiddleName == null
                        ? x.StaffMember.FirstName +
                          " " +
                          x.StaffMember.LastName
                        : x.StaffMember.FirstName +
                          " " +
                          x.StaffMember.MiddleName +
                          " " +
                          x.StaffMember.LastName,
                    x.StaffMember.EmploymentType))
                .ToListAsync(
                    cancellationToken);

        var availability =
            await _database.StaffAvailability
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .Select(x => new AvailabilityRecord(
                    x.StaffMemberId,
                    x.DayOfWeek,
                    x.StartTime,
                    x.EndTime))
                .ToListAsync(
                    cancellationToken);

        var orderedRequirements =
            requirements
                .OrderBy(x =>
                    EstimateFlexibility(
                        x,
                        assignments,
                        availability,
                        slots))
                .ThenByDescending(x =>
                    x.PeriodsPerWeek)
                .ThenBy(x =>
                    x.ClassGroupName)
                .ThenBy(x =>
                    x.SubjectName)
                .ToList();

        var classBusy =
            new HashSet<string>();

        var teacherBusy =
            new HashSet<string>();

        var subjectDayCount =
            new Dictionary<string, int>();

        var teacherLoad =
            new Dictionary<Guid, int>();

        var planned =
            new List<PlannedEntry>();

        foreach (
            var requirement
            in orderedRequirements)
        {
            var matchingAssignments =
                assignments
                    .Where(x =>
                        x.ClassGroupId ==
                            requirement.ClassGroupId &&
                        x.SubjectId ==
                            requirement.SubjectId)
                    .ToList();

            if (matchingAssignments.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{requirement.ClassGroupName} — {requirement.SubjectName} has no assigned teacher.");
            }

            for (
                var lessonIndex = 0;
                lessonIndex <
                    requirement.PeriodsPerWeek;
                lessonIndex++)
            {
                Candidate? best = null;

                foreach (
                    var assignment
                    in matchingAssignments)
                {
                    foreach (
                        var slot
                        in slots)
                    {
                        if (!TeacherCanUseSlot(
                                assignment,
                                slot,
                                availability))
                        {
                            continue;
                        }

                        var classKey =
                            BusyKey(
                                requirement.ClassGroupId,
                                slot);

                        if (classBusy.Contains(
                                classKey))
                        {
                            continue;
                        }

                        var teacherKey =
                            BusyKey(
                                assignment.StaffMemberId,
                                slot);

                        if (teacherBusy.Contains(
                                teacherKey))
                        {
                            continue;
                        }

                        var subjectDayKey =
                            $"{requirement.ClassGroupId:N}|{requirement.SubjectId:N}|{(int)slot.DayOfWeek}";

                        var sameSubjectToday =
                            subjectDayCount.TryGetValue(
                                subjectDayKey,
                                out var count)
                                ? count
                                : 0;

                        var currentTeacherLoad =
                            teacherLoad.TryGetValue(
                                assignment.StaffMemberId,
                                out var load)
                                ? load
                                : 0;

                        var score =
                            sameSubjectToday * 10000 +
                            currentTeacherLoad * 10 +
                            slot.PeriodNumber;

                        if (best is null ||
                            score < best.Score)
                        {
                            best =
                                new Candidate(
                                    assignment,
                                    slot,
                                    score);
                        }
                    }
                }

                if (best is null)
                {
                    var availableByTeacher = matchingAssignments
                        .Select(assignment => new
                        {
                            assignment.StaffName,
                            Count = slots.Count(slot => TeacherCanUseSlot(assignment, slot, availability))
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList();
                    var bestAvailability = availableByTeacher.FirstOrDefault();
                    throw new InvalidOperationException(
                        $"Unable to place all {requirement.PeriodsPerWeek} weekly periods for {requirement.ClassGroupName} — {requirement.SubjectName}. Placed {lessonIndex} of {requirement.PeriodsPerWeek}; assigned teacher {bestAvailability?.StaffName ?? "unknown"} has {bestAvailability?.Count ?? 0} compatible timetable slots. Review teacher availability or timetable capacity.");
                }

                var selectedAssignment =
                    best.Assignment;

                var selectedSlot =
                    best.Slot;

                classBusy.Add(
                    BusyKey(
                        requirement.ClassGroupId,
                        selectedSlot));

                teacherBusy.Add(
                    BusyKey(
                        selectedAssignment.StaffMemberId,
                        selectedSlot));

                var selectedSubjectDayKey =
                    $"{requirement.ClassGroupId:N}|{requirement.SubjectId:N}|{(int)selectedSlot.DayOfWeek}";

                subjectDayCount[
                    selectedSubjectDayKey] =
                    subjectDayCount.TryGetValue(
                        selectedSubjectDayKey,
                        out var existingCount)
                        ? existingCount + 1
                        : 1;

                teacherLoad[
                    selectedAssignment.StaffMemberId] =
                    teacherLoad.TryGetValue(
                        selectedAssignment.StaffMemberId,
                        out var existingLoad)
                        ? existingLoad + 1
                        : 1;

                planned.Add(
                    new PlannedEntry(
                        requirement,
                        selectedAssignment,
                        selectedSlot));
            }
        }

        await using var transaction =
            await _database.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var timetable =
                await _database.GeneratedTimetables
                    .Include(x => x.Entries)
                    .SingleOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.AcademicTermId ==
                                request.AcademicTermId,
                        cancellationToken);

            if (timetable is null)
            {
                timetable =
                    new GeneratedTimetable(
                        tenantId,
                        session.Id,
                        request.AcademicTermId);

                _database.GeneratedTimetables.Add(
                    timetable);

                await _database.SaveChangesAsync(
                    cancellationToken);
            }
            else
            {
                _database.GeneratedTimetableEntries
                    .RemoveRange(
                        timetable.Entries);

                timetable.MarkRegenerated();

                await _database.SaveChangesAsync(
                    cancellationToken);
            }

            foreach (
                var item
                in planned)
            {
                _database.GeneratedTimetableEntries.Add(
                    new GeneratedTimetableEntry(
                        tenantId,
                        timetable.Id,
                        item.Requirement.ClassGroupId,
                        item.Requirement.SubjectId,
                        item.Assignment.StaffMemberId,
                        item.Slot.DayOfWeek,
                        item.Slot.PeriodNumber,
                        item.Slot.StartTime,
                        item.Slot.EndTime));
            }

            await _database.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return await GetAsync(
                       request.AcademicTermId,
                       cancellationToken)
                   ?? throw new InvalidOperationException(
                       "Generated timetable could not be loaded.");
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<GeneratedTimetableResult?> GetAsync(
        Guid academicTermId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var timetable =
            await _database.GeneratedTimetables
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.AcademicTermId ==
                            academicTermId,
                    cancellationToken);

        if (timetable is null)
        {
            return null;
        }

        var rawEntries =
            await _database.GeneratedTimetableEntries
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.GeneratedTimetableId ==
                        timetable.Id)
                .ToListAsync(
                    cancellationToken);

        var classIds =
            rawEntries
                .Select(x =>
                    x.ClassGroupId)
                .Distinct()
                .ToList();

        var subjectIds =
            rawEntries
                .Select(x =>
                    x.SubjectId)
                .Distinct()
                .ToList();

        var staffIds =
            rawEntries
                .Select(x =>
                    x.StaffMemberId)
                .Distinct()
                .ToList();

        var classes =
            await _database.ClassGroups
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    classIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    AcademicLevelName =
                        x.AcademicLevel.Name
                })
                .ToDictionaryAsync(
                    x => x.Id,
                    cancellationToken);

        var subjects =
            await _database.Subjects
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    subjectIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Name
                })
                .ToDictionaryAsync(
                    x => x.Id,
                    cancellationToken);

        var staff =
            await _database.StaffMembers
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    staffIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,

                    Name =
                        x.MiddleName == null
                            ? x.FirstName +
                              " " +
                              x.LastName
                            : x.FirstName +
                              " " +
                              x.MiddleName +
                              " " +
                              x.LastName
                })
                .ToDictionaryAsync(
                    x => x.Id,
                    cancellationToken);

        var entries =
            rawEntries
                .OrderBy(x =>
                    DayOrder(
                        x.DayOfWeek))
                .ThenBy(x =>
                    x.PeriodNumber)
                .ThenBy(x =>
                    classes.TryGetValue(
                        x.ClassGroupId,
                        out var item)
                        ? item.Name
                        : "")
                .Select(x =>
                {
                    classes.TryGetValue(
                        x.ClassGroupId,
                        out var classGroup);

                    subjects.TryGetValue(
                        x.SubjectId,
                        out var subject);

                    staff.TryGetValue(
                        x.StaffMemberId,
                        out var teacher);

                    return new GeneratedTimetableEntryResult(
                        x.Id,
                        x.ClassGroupId,
                        classGroup?.Name ??
                            "Unknown Class",
                        classGroup?.AcademicLevelName ??
                            "",
                        x.SubjectId,
                        subject?.Name ??
                            "Unknown Subject",
                        x.StaffMemberId,
                        teacher?.Name ??
                            "Unknown Teacher",
                        x.DayOfWeek,
                        x.PeriodNumber,
                        x.StartTime,
                        x.EndTime);
                })
                .ToList();

        return new GeneratedTimetableResult(
            timetable.Id,
            timetable.AcademicSessionId,
            timetable.AcademicTermId,
            timetable.GeneratedAtUtc,
            entries.Count,
            entries);
    }

    public async Task ResetAsync(Guid academicTermId, CancellationToken cancellationToken = default)
    {
        if (academicTermId == Guid.Empty)
            throw new InvalidOperationException("Academic term is required.");

        var tenantId = _tenantContext.TenantId;
        var sessionId = await _database.AcademicSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsCurrent && x.IsActive)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (sessionId == Guid.Empty)
            throw new InvalidOperationException("A current academic session is required.");

        var validTerm = await _database.AcademicTerms.AsNoTracking().AnyAsync(x =>
            x.Id == academicTermId && x.TenantId == tenantId && x.AcademicSessionId == sessionId && x.IsActive,
            cancellationToken);
        if (!validTerm)
            throw new InvalidOperationException("The selected academic term was not found in the current academic session.");

        await using var transaction = await _database.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var timetable = await _database.GeneratedTimetables.SingleOrDefaultAsync(x =>
                x.TenantId == tenantId && x.AcademicSessionId == sessionId && x.AcademicTermId == academicTermId,
                cancellationToken);
            if (timetable is not null)
            {
                _database.GeneratedTimetables.Remove(timetable);
                await _database.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static List<TeachingSlot> BuildSlots(
        TimetableSettings settings)
    {
        var slots =
            new List<TeachingSlot>();

        foreach (
            var day
            in settings.Days
                .Where(x =>
                    x.IsActive)
                .OrderBy(x =>
                    DayOrder(
                        x.DayOfWeek)))
        {
            var blocks =
                day.NonTeachingBlocks
                    .Where(x =>
                        x.IsActive)
                    .OrderBy(x =>
                        x.StartTime)
                    .ToList();

            var periodNumber = 1;
            var cursor =
                day.StartTime;

            foreach (
                var block
                in blocks)
            {
                AddSegmentSlots(
                    slots,
                    day.DayOfWeek,
                    ref periodNumber,
                    cursor,
                    block.StartTime,
                    settings.PeriodDurationMinutes);

                cursor =
                    block.EndTime;
            }

            AddSegmentSlots(
                slots,
                day.DayOfWeek,
                ref periodNumber,
                cursor,
                day.EndTime,
                settings.PeriodDurationMinutes);
        }

        return slots;
    }

    private static void AddSegmentSlots(
        ICollection<TeachingSlot> slots,
        DayOfWeek day,
        ref int periodNumber,
        TimeOnly start,
        TimeOnly end,
        int durationMinutes)
    {
        var cursor = start;

        while (true)
        {
            var next =
                cursor.AddMinutes(
                    durationMinutes);

            if (next > end)
            {
                break;
            }

            slots.Add(
                new TeachingSlot(
                    day,
                    periodNumber,
                    cursor,
                    next));

            periodNumber++;
            cursor = next;
        }
    }

    private static bool TeacherCanUseSlot(
        AssignmentRecord assignment,
        TeachingSlot slot,
        IReadOnlyCollection<AvailabilityRecord> availability)
    {
        if (string.Equals(
                assignment.EmploymentType,
                "Full-Time",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return availability.Any(x =>
            x.StaffMemberId ==
                assignment.StaffMemberId &&
            x.DayOfWeek ==
                slot.DayOfWeek &&
            x.StartTime <=
                slot.StartTime &&
            x.EndTime >=
                slot.EndTime);
    }

    private static int EstimateFlexibility(
        RequirementRecord requirement,
        IReadOnlyCollection<AssignmentRecord> assignments,
        IReadOnlyCollection<AvailabilityRecord> availability,
        IReadOnlyCollection<TeachingSlot> slots)
    {
        var matching =
            assignments
                .Where(x =>
                    x.ClassGroupId ==
                        requirement.ClassGroupId &&
                    x.SubjectId ==
                        requirement.SubjectId)
                .ToList();

        if (matching.Count == 0)
        {
            return 0;
        }

        return matching.Sum(
            assignment =>
                slots.Count(
                    slot =>
                        TeacherCanUseSlot(
                            assignment,
                            slot,
                            availability)));
    }

    private static string BusyKey(
        Guid entityId,
        TeachingSlot slot)
    {
        return $"{entityId:N}|{(int)slot.DayOfWeek}|{slot.PeriodNumber}";
    }

    private static int DayOrder(
        DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            DayOfWeek.Sunday => 7,
            _ => 8
        };
    }

    private sealed record RequirementRecord(
        Guid ClassGroupId,
        string ClassGroupName,
        string AcademicLevelName,
        Guid SubjectId,
        string SubjectName,
        int PeriodsPerWeek);

    private sealed record AssignmentRecord(
        Guid ClassGroupId,
        Guid SubjectId,
        Guid StaffMemberId,
        string StaffName,
        string EmploymentType);

    private sealed record AvailabilityRecord(
        Guid StaffMemberId,
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        TimeOnly EndTime);

    private sealed record TeachingSlot(
        DayOfWeek DayOfWeek,
        int PeriodNumber,
        TimeOnly StartTime,
        TimeOnly EndTime);

    private sealed record Candidate(
        AssignmentRecord Assignment,
        TeachingSlot Slot,
        int Score);

    private sealed record PlannedEntry(
        RequirementRecord Requirement,
        AssignmentRecord Assignment,
        TeachingSlot Slot);
}
