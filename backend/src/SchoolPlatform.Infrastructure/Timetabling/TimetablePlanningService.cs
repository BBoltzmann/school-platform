using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Timetabling;
using SchoolPlatform.Domain.Timetabling;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Timetabling;

public sealed class TimetablePlanningService
    : ITimetablePlanningService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public TimetablePlanningService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<TimetablePlanningSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var currentSession =
            await _database.AcademicSessions
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsCurrent &&
                    x.IsActive)
                .OrderByDescending(x => x.StartDate)
                .Select(x => new TimetableSessionOption(
                    x.Id,
                    x.Name,
                    x.StartDate,
                    x.EndDate))
                .FirstOrDefaultAsync(cancellationToken);

        TimetableSettingsResult? settings = null;

        if (currentSession is not null)
        {
            settings =
                await GetSettingsResultAsync(
                    tenantId,
                    currentSession.Id,
                    cancellationToken);
        }

        var classes =
            await _database.ClassGroups
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive &&
                    x.AcademicLevel.IsActive)
                .OrderBy(x =>
                    x.AcademicLevel.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new TimetableClassOption(
                    x.Id,
                    x.Name,
                    x.AcademicLevelId,
                    x.AcademicLevel.Name,
                    x.UsesCustomSubjectOffering,
                    x.ClassSubjects.Select(cs => cs.SubjectId).ToList()))
                .ToListAsync(cancellationToken);

        var subjects =
            await _database.Subjects
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new TimetableSubjectOption(
                    x.Id,
                    x.Name))
                .ToListAsync(cancellationToken);

        return new TimetablePlanningSetupResult(
            currentSession,
            settings,
            classes,
            subjects);
    }

    public async Task<TimetableSettingsResult> SaveSettingsAsync(
        SaveTimetableSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var session =
            await GetCurrentSessionAsync(
                tenantId,
                cancellationToken);

        ValidateSettings(request);

        await using var transaction =
            await _database.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var settings =
                await _database.TimetableSettings
                    .Include(x => x.Days)
                    .ThenInclude(x =>
                        x.NonTeachingBlocks)
                    .SingleOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.AcademicSessionId == session.Id,
                        cancellationToken);

            if (settings is null)
            {
                settings =
                    new TimetableSettings(
                        tenantId,
                        session.Id,
                        request.PeriodDurationMinutes);

                _database.TimetableSettings.Add(
                    settings);
            }
            else
            {
                settings.UpdatePeriodDuration(
                    request.PeriodDurationMinutes);

                settings.Activate();
            }

            var requestedDays =
                request.Days.ToDictionary(
                    x => x.DayOfWeek);

            foreach (var existingDay in settings.Days)
            {
                if (!requestedDays.ContainsKey(
                        existingDay.DayOfWeek))
                {
                    existingDay.Deactivate();

                    foreach (
                        var block
                        in existingDay.NonTeachingBlocks)
                    {
                        block.Deactivate();
                    }
                }
            }

            foreach (var requestedDay in request.Days)
            {
                var day =
                    settings.Days
                        .FirstOrDefault(x =>
                            x.DayOfWeek ==
                            requestedDay.DayOfWeek);

                if (day is null)
                {
                    day =
                        new TimetableDay(
                            tenantId,
                            settings.Id,
                            requestedDay.DayOfWeek,
                            requestedDay.StartTime,
                            requestedDay.EndTime);

                    settings.Days.Add(day);
                }
                else
                {
                    day.Update(
                        requestedDay.StartTime,
                        requestedDay.EndTime);
                }

                foreach (
                    var existingBlock
                    in day.NonTeachingBlocks)
                {
                    existingBlock.Deactivate();
                }

                foreach (
                    var requestedBlock
                    in requestedDay
                        .NonTeachingBlocks
                        .OrderBy(x =>
                            x.SortOrder)
                        .ThenBy(x =>
                            x.StartTime))
                {
                    var block =
                        new TimetableNonTeachingBlock(
                            tenantId,
                            day.Id,
                            requestedBlock.Name,
                            requestedBlock.StartTime,
                            requestedBlock.EndTime,
                            requestedBlock.SortOrder);

                    _database.TimetableNonTeachingBlocks.Add(block);
                }
            }

            await _database.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }

        return await GetSettingsResultAsync(
                   tenantId,
                   session.Id,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Timetable settings could not be loaded.");
    }

    public async Task<ClassSubjectRequirementsResult> GetClassRequirementsAsync(
        Guid classGroupId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var session =
            await GetCurrentSessionAsync(
                tenantId,
                cancellationToken);

        var classGroup =
            await _database.ClassGroups
                .AsNoTracking()
                .Where(x =>
                    x.Id == classGroupId &&
                    x.TenantId == tenantId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.AcademicLevelId,
                    AcademicLevelName =
                        x.AcademicLevel.Name
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (classGroup is null)
        {
            throw new InvalidOperationException(
                "Class was not found.");
        }

        var requirements =
            await _database.ClassSubjectRequirements
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.ClassGroupId == classGroupId &&
                    x.IsActive &&
                    (!x.ClassGroup.UsesCustomSubjectOffering ||
                     x.ClassGroup.ClassSubjects.Any(cs => cs.SubjectId == x.SubjectId)))
                .OrderBy(x =>
                    x.Subject.Name)
                .Select(x =>
                    new ClassSubjectRequirementResult(
                        x.Id,
                        x.SubjectId,
                        x.Subject.Name,
                        x.PeriodsPerWeek))
                .ToListAsync(
                    cancellationToken);

        var parallelGroups = await _database.ParallelSubjectGroups
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ClassGroupId == classGroupId &&
                x.AcademicSessionId == session.Id &&
                x.IsActive)
            .Select(x => x.Members
                .Select(member => member.ClassSubject.SubjectId)
                .ToList())
            .ToListAsync(cancellationToken);

        var capacity = ParallelTimetableCapacity.Calculate(
            requirements.ToDictionary(x => x.SubjectId, x => x.PeriodsPerWeek),
            parallelGroups);

        return new ClassSubjectRequirementsResult(
            classGroup.Id,
            classGroup.Name,
            classGroup.AcademicLevelId,
            classGroup.AcademicLevelName,
            session.Id,
            session.Name,
            capacity.Raw,
            capacity.Effective,
            capacity.Savings,
            requirements);
    }

    public async Task<ClassSubjectRequirementsResult> SaveClassRequirementsAsync(
        Guid classGroupId,
        SaveClassSubjectRequirementsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var session =
            await GetCurrentSessionAsync(
                tenantId,
                cancellationToken);

        var classOffering =
            await _database.ClassGroups
                .AsNoTracking()
                .Where(x => x.Id == classGroupId && x.TenantId == tenantId && x.IsActive)
                .Select(x => new { x.UsesCustomSubjectOffering, OfferedSubjectIds = x.ClassSubjects.Select(cs => cs.SubjectId).ToList() })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (classOffering is null)
        {
            throw new InvalidOperationException(
                "Class was not found.");
        }

        ValidateRequirements(
            request);

        var subjectIds =
            request.Requirements
                .Select(x =>
                    x.SubjectId)
                .Distinct()
                .ToList();

        if (subjectIds.Count > 0)
        {
            var validSubjectIds =
                await _database.Subjects
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.IsActive &&
                        subjectIds.Contains(x.Id) &&
                        (!classOffering.UsesCustomSubjectOffering || classOffering.OfferedSubjectIds.Contains(x.Id)))
                    .Select(x => x.Id)
                    .ToListAsync(
                        cancellationToken);

            var missing =
                subjectIds
                    .Except(validSubjectIds)
                    .Any();

            if (missing)
            {
                throw new InvalidOperationException(
                    "One or more selected subjects were not found.");
            }
        }

        var existing =
            await _database.ClassSubjectRequirements
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AcademicSessionId == session.Id &&
                    x.ClassGroupId == classGroupId)
                .ToListAsync(
                    cancellationToken);

        var requested =
            request.Requirements
                .ToDictionary(
                    x => x.SubjectId);

        foreach (
            var requirement
            in existing)
        {
            if (requested.TryGetValue(
                    requirement.SubjectId,
                    out var item))
            {
                requirement.Update(
                    item.PeriodsPerWeek);

                requested.Remove(
                    requirement.SubjectId);
            }
            else
            {
                requirement.Deactivate();
            }
        }

        foreach (
            var item
            in requested.Values)
        {
            var requirement =
                new ClassSubjectRequirement(
                    tenantId,
                    session.Id,
                    classGroupId,
                    item.SubjectId,
                    item.PeriodsPerWeek);

            _database.ClassSubjectRequirements.Add(
                requirement);
        }

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetClassRequirementsAsync(
            classGroupId,
            cancellationToken);
    }

    private async Task<TimetableSettingsResult?> GetSettingsResultAsync(
        Guid tenantId,
        Guid academicSessionId,
        CancellationToken cancellationToken)
    {
        var settings =
            await _database.TimetableSettings
                .AsNoTracking()
                .Include(x =>
                    x.AcademicSession)
                .Include(x =>
                    x.Days)
                .ThenInclude(x =>
                    x.NonTeachingBlocks)
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.AcademicSessionId ==
                            academicSessionId &&
                        x.IsActive,
                    cancellationToken);

        if (settings is null)
        {
            return null;
        }

        var days =
            settings.Days
                .Where(x =>
                    x.IsActive)
                .OrderBy(x =>
                    DayOrder(
                        x.DayOfWeek))
                .Select(x =>
                    new TimetableDayResult(
                        x.Id,
                        x.DayOfWeek,
                        x.StartTime,
                        x.EndTime,
                        x.NonTeachingBlocks
                            .Where(block =>
                                block.IsActive)
                            .OrderBy(block =>
                                block.SortOrder)
                            .ThenBy(block =>
                                block.StartTime)
                            .Select(block =>
                                new TimetableNonTeachingBlockResult(
                                    block.Id,
                                    block.Name,
                                    block.StartTime,
                                    block.EndTime,
                                    block.SortOrder))
                            .ToList()))
                .ToList();

        return new TimetableSettingsResult(
            settings.Id,
            settings.AcademicSessionId,
            settings.AcademicSession.Name,
            settings.PeriodDurationMinutes,
            days);
    }

    private async Task<CurrentSessionRecord> GetCurrentSessionAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var session =
            await _database.AcademicSessions
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.IsCurrent &&
                    x.IsActive)
                .Select(x =>
                    new CurrentSessionRecord(
                        x.Id,
                        x.Name))
                .FirstOrDefaultAsync(
                    cancellationToken);

        return session
            ?? throw new InvalidOperationException(
                "A current academic session is required.");
    }

    private static void ValidateSettings(
        SaveTimetableSettingsRequest request)
    {
        if (request.PeriodDurationMinutes < 10 ||
            request.PeriodDurationMinutes > 180)
        {
            throw new InvalidOperationException(
                "Period duration must be between 10 and 180 minutes.");
        }

        if (request.Days is null ||
            request.Days.Count == 0)
        {
            throw new InvalidOperationException(
                "Select at least one school day.");
        }

        var duplicateDay =
            request.Days
                .GroupBy(x =>
                    x.DayOfWeek)
                .FirstOrDefault(x =>
                    x.Count() > 1);

        if (duplicateDay is not null)
        {
            throw new InvalidOperationException(
                $"{duplicateDay.Key} appears more than once.");
        }

        foreach (var day in request.Days)
        {
            if (day.StartTime >=
                day.EndTime)
            {
                throw new InvalidOperationException(
                    $"{day.DayOfWeek}: school start time must be earlier than closing time.");
            }

            var blocks =
                (day.NonTeachingBlocks ??
                 Array.Empty<SaveTimetableNonTeachingBlockRequest>())
                .OrderBy(x =>
                    x.StartTime)
                .ToList();

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(
                        block.Name))
                {
                    throw new InvalidOperationException(
                        $"{day.DayOfWeek}: every non-teaching block must have a name.");
                }

                if (block.StartTime >=
                    block.EndTime)
                {
                    throw new InvalidOperationException(
                        $"{day.DayOfWeek} — {block.Name}: start time must be earlier than end time.");
                }

                if (block.StartTime <
                        day.StartTime ||
                    block.EndTime >
                        day.EndTime)
                {
                    throw new InvalidOperationException(
                        $"{day.DayOfWeek} — {block.Name}: the block must fall inside the school day.");
                }
            }

            for (var index = 1;
                 index < blocks.Count;
                 index++)
            {
                var previous =
                    blocks[index - 1];

                var current =
                    blocks[index];

                if (current.StartTime <
                    previous.EndTime)
                {
                    throw new InvalidOperationException(
                        $"{day.DayOfWeek}: '{previous.Name}' overlaps '{current.Name}'.");
                }
            }

            ValidateTeachingSegments(
                day,
                blocks,
                request.PeriodDurationMinutes);
        }
    }

    private static void ValidateTeachingSegments(
        SaveTimetableDayRequest day,
        IReadOnlyCollection<SaveTimetableNonTeachingBlockRequest> blocks,
        int periodDurationMinutes)
    {
        var cursor =
            day.StartTime;

        foreach (
            var block
            in blocks.OrderBy(x =>
                x.StartTime))
        {
            ValidateTeachingSegment(
                day.DayOfWeek,
                cursor,
                block.StartTime,
                periodDurationMinutes);

            cursor =
                block.EndTime;
        }

        ValidateTeachingSegment(
            day.DayOfWeek,
            cursor,
            day.EndTime,
            periodDurationMinutes);
    }

    private static void ValidateTeachingSegment(
        DayOfWeek day,
        TimeOnly startTime,
        TimeOnly endTime,
        int periodDurationMinutes)
    {
        if (startTime == endTime)
        {
            return;
        }

        var minutes =
            (int)(
                endTime.ToTimeSpan() -
                startTime.ToTimeSpan())
            .TotalMinutes;

        if (minutes < 0)
        {
            throw new InvalidOperationException(
                $"{day}: timetable times are invalid.");
        }

        if (minutes %
            periodDurationMinutes != 0)
        {
            throw new InvalidOperationException(
                $"{day}: the teaching time from {startTime:HH\\:mm} to {endTime:HH\\:mm} does not divide evenly into {periodDurationMinutes}-minute periods. Adjust the break or school-day time.");
        }
    }

    private static void ValidateRequirements(
        SaveClassSubjectRequirementsRequest request)
    {
        if (request.Requirements is null)
        {
            throw new InvalidOperationException(
                "Subject requirements are required.");
        }

        var duplicate =
            request.Requirements
                .GroupBy(x =>
                    x.SubjectId)
                .FirstOrDefault(x =>
                    x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                "The same subject cannot appear more than once for a class.");
        }

        foreach (
            var requirement
            in request.Requirements)
        {
            if (requirement.PeriodsPerWeek <= 0)
            {
                throw new InvalidOperationException(
                    "Periods per week must be greater than zero.");
            }

            if (requirement.PeriodsPerWeek > 50)
            {
                throw new InvalidOperationException(
                    "Periods per week cannot exceed 50.");
            }
        }
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

    private sealed record CurrentSessionRecord(
        Guid Id,
        string Name);
}
