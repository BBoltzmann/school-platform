using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Timetabling;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Timetabling;

public sealed class TimetableReadinessService
    : ITimetableReadinessService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public TimetableReadinessService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<TimetableReadinessResult> GetReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

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
                    cancellationToken);

        if (session is null)
        {
            return new TimetableReadinessResult(
                false,
                Guid.Empty,
                "",
                0,
                0,
                0,
                0,
                0,
                new[]
                {
                    new TimetableReadinessIssueResult(
                        "CURRENT_SESSION_MISSING",
                        "Error",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        "A current academic session must be configured.")
                });
        }

        var issues =
            new List<TimetableReadinessIssueResult>();

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
                    cancellationToken);

        var weeklyCapacity = 0;

        if (settings is null)
        {
            issues.Add(
                new TimetableReadinessIssueResult(
                    "TIMETABLE_STRUCTURE_MISSING",
                    "Error",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "Timetable structure has not been configured."));
        }
        else
        {
            var activeDays =
                settings.Days
                    .Where(x => x.IsActive)
                    .ToList();

            if (activeDays.Count == 0)
            {
                issues.Add(
                    new TimetableReadinessIssueResult(
                        "SCHOOL_DAYS_MISSING",
                        "Error",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        "No active school days have been configured."));
            }
            else
            {
                foreach (var day in activeDays)
                {
                    var totalMinutes =
                        (int)(
                            day.EndTime.ToTimeSpan() -
                            day.StartTime.ToTimeSpan())
                        .TotalMinutes;

                    var blockedMinutes =
                        day.NonTeachingBlocks
                            .Where(x =>
                                x.IsActive)
                            .Sum(block =>
                                (int)(
                                    block.EndTime.ToTimeSpan() -
                                    block.StartTime.ToTimeSpan())
                                .TotalMinutes);

                    var teachingMinutes =
                        Math.Max(
                            0,
                            totalMinutes -
                            blockedMinutes);

                    weeklyCapacity +=
                        teachingMinutes /
                        settings.PeriodDurationMinutes;
                }
            }
        }

        var classes =
            await _database.ClassGroups
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive &&
                    x.AcademicLevel.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    AcademicLevelName =
                        x.AcademicLevel.Name
                })
                .ToListAsync(
                    cancellationToken);

        var requirements =
            await _database.ClassSubjectRequirements
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.IsActive &&
                    (!x.ClassGroup.UsesCustomSubjectOffering ||
                     x.ClassGroup.ClassSubjects.Any(cs => cs.SubjectId == x.SubjectId)))
                .Select(x => new
                {
                    x.Id,
                    x.ClassGroupId,
                    ClassGroupName =
                        x.ClassGroup.Name,
                    x.SubjectId,
                    SubjectName =
                        x.Subject.Name,
                    x.PeriodsPerWeek
                })
                .ToListAsync(
                    cancellationToken);

        var parallelGroups = await _database.ParallelSubjectGroups
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AcademicSessionId == session.Id && x.IsActive)
            .Select(x => new { x.ClassGroupId, Members = x.Members.Select(m => m.ClassSubject.SubjectId).ToList() })
            .ToListAsync(cancellationToken);

        var assignments =
            await _database.TeachingAssignments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.IsActive &&
                    x.StaffMember.IsActive &&
                    x.StaffMember.IsTeachingStaff)
                .Select(x => new
                {
                    x.Id,
                    x.ClassGroupId,
                    x.SubjectId,
                    x.StaffMemberId,

                    StaffName =
                        x.StaffMember.MiddleName == null
                            ? x.StaffMember.FirstName +
                              " " +
                              x.StaffMember.LastName
                            : x.StaffMember.FirstName +
                              " " +
                              x.StaffMember.MiddleName +
                              " " +
                              x.StaffMember.LastName,

                    x.StaffMember.EmploymentType
                })
                .ToListAsync(
                    cancellationToken);

        var availability =
            await _database.StaffAvailability
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.StaffMemberId,
                    x.DayOfWeek
                })
                .ToListAsync(
                    cancellationToken);

        var timetableDays =
            settings?.Days
                .Where(x => x.IsActive)
                .Select(x => x.DayOfWeek)
                .ToHashSet()
            ?? new HashSet<DayOfWeek>();

        var coveredRequirementCount = 0;
        var readyClasses = 0;

        foreach (var classGroup in classes)
        {
            var classRequirements =
                requirements
                    .Where(x =>
                        x.ClassGroupId ==
                        classGroup.Id)
                    .ToList();

            var classHasError = false;

            if (classRequirements.Count == 0)
            {
                classHasError = true;

                issues.Add(
                    new TimetableReadinessIssueResult(
                        "CLASS_REQUIREMENTS_MISSING",
                        "Error",
                        classGroup.Id,
                        classGroup.Name,
                        null,
                        null,
                        null,
                        null,
                        $"{classGroup.AcademicLevelName} — {classGroup.Name} has no weekly subject requirements."));
            }

            var groupedSubjects = parallelGroups
                .Where(g => g.ClassGroupId == classGroup.Id)
                .SelectMany(g => g.Members)
                .ToHashSet();
            var classPeriodTotal = classRequirements
                .Where(x => !groupedSubjects.Contains(x.SubjectId))
                .Sum(x => x.PeriodsPerWeek)
                + parallelGroups.Where(g => g.ClassGroupId == classGroup.Id)
                    .Select(g => classRequirements.Where(r => g.Members.Contains(r.SubjectId)).Select(r => r.PeriodsPerWeek).DefaultIfEmpty(0).Max())
                    .Sum();

            if (weeklyCapacity > 0 &&
                classPeriodTotal >
                weeklyCapacity)
            {
                classHasError = true;

                issues.Add(
                    new TimetableReadinessIssueResult(
                        "CLASS_OVER_CAPACITY",
                        "Error",
                        classGroup.Id,
                        classGroup.Name,
                        null,
                        null,
                        null,
                        null,
                        $"{classGroup.AcademicLevelName} — {classGroup.Name} requires {classPeriodTotal} periods per week, but only {weeklyCapacity} periods are available."));
            }

            foreach (
                var requirement
                in classRequirements)
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
                    classHasError = true;

                    issues.Add(
                        new TimetableReadinessIssueResult(
                            "SUBJECT_TEACHER_MISSING",
                            "Error",
                            requirement.ClassGroupId,
                            requirement.ClassGroupName,
                            requirement.SubjectId,
                            requirement.SubjectName,
                            null,
                            null,
                            $"{requirement.ClassGroupName} — {requirement.SubjectName} has no assigned teacher."));

                    continue;
                }

                coveredRequirementCount++;

                foreach (
                    var assignment
                    in matchingAssignments)
                {
                    if (string.Equals(
                            assignment.EmploymentType,
                            "Full-Time",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var teacherAvailability =
                        availability
                            .Where(x =>
                                x.StaffMemberId ==
                                assignment.StaffMemberId)
                            .ToList();

                    if (teacherAvailability.Count == 0)
                    {
                        classHasError = true;

                        AddAvailabilityIssueIfMissing(
                            issues,
                            assignment.StaffMemberId,
                            assignment.StaffName,
                            requirement.ClassGroupId,
                            requirement.ClassGroupName,
                            requirement.SubjectId,
                            requirement.SubjectName,
                            "No working days have been configured.");
                    }
                    else if (
                        timetableDays.Count > 0 &&
                        !teacherAvailability.Any(x =>
                            timetableDays.Contains(
                                x.DayOfWeek)))
                    {
                        classHasError = true;

                        AddAvailabilityIssueIfMissing(
                            issues,
                            assignment.StaffMemberId,
                            assignment.StaffName,
                            requirement.ClassGroupId,
                            requirement.ClassGroupName,
                            requirement.SubjectId,
                            requirement.SubjectName,
                            "Their availability does not fall on any configured school day.");
                    }
                }
            }

            if (!classHasError &&
                classRequirements.Count > 0)
            {
                readyClasses++;
            }
        }

        var canGenerate =
            issues.All(x =>
                !string.Equals(
                    x.Severity,
                    "Error",
                    StringComparison.OrdinalIgnoreCase));

        return new TimetableReadinessResult(
            canGenerate,
            session.Id,
            session.Name,
            weeklyCapacity,
            classes.Count,
            readyClasses,
            requirements.Count,
            coveredRequirementCount,
            issues);
    }

    private static void AddAvailabilityIssueIfMissing(
        ICollection<TimetableReadinessIssueResult> issues,
        Guid staffId,
        string staffName,
        Guid classGroupId,
        string classGroupName,
        Guid subjectId,
        string subjectName,
        string reason)
    {
        var alreadyExists =
            issues.Any(x =>
                x.Code ==
                    "STAFF_AVAILABILITY_MISSING" &&
                x.StaffMemberId ==
                    staffId);

        if (alreadyExists)
        {
            return;
        }

        issues.Add(
            new TimetableReadinessIssueResult(
                "STAFF_AVAILABILITY_MISSING",
                "Error",
                classGroupId,
                classGroupName,
                subjectId,
                subjectName,
                staffId,
                staffName,
                $"{staffName} is not Full-Time and cannot yet be scheduled. {reason}"));
    }
}
