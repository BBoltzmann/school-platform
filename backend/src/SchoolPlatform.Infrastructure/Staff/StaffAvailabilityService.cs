using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Staff;
using SchoolPlatform.Domain.Staff;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Staff;

public sealed class StaffAvailabilityService
    : IStaffAvailabilityService
{
    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public StaffAvailabilityService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyCollection<StaffAvailabilityResult>> GetAsync(
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureStaffExistsAsync(
            tenantId,
            staffId,
            cancellationToken);

        var items =
            await _database.StaffAvailability
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.StaffMemberId == staffId &&
                    x.IsActive)
                .Select(x => new StaffAvailabilityResult(
                    x.Id,
                    x.DayOfWeek,
                    x.StartTime,
                    x.EndTime,
                    x.IsActive))
                .ToListAsync(cancellationToken);

        return items
            .OrderBy(x => SortDay(x.DayOfWeek))
            .ToList();
    }

    public async Task<IReadOnlyCollection<StaffAvailabilityResult>> SaveAsync(
        Guid staffId,
        SaveStaffAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureStaffExistsAsync(
            tenantId,
            staffId,
            cancellationToken);

        ValidateRequest(request);

        var requestedDays =
            request.Days
                .ToDictionary(
                    x => x.DayOfWeek);

        var existing =
            await _database.StaffAvailability
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.StaffMemberId == staffId)
                .ToListAsync(cancellationToken);

        foreach (var availability in existing)
        {
            if (requestedDays.TryGetValue(
                    availability.DayOfWeek,
                    out var requested))
            {
                availability.Update(
                    requested.StartTime,
                    requested.EndTime);

                requestedDays.Remove(
                    availability.DayOfWeek);
            }
            else
            {
                availability.Deactivate();
            }
        }

        foreach (var requested in requestedDays.Values)
        {
            var availability =
                new StaffAvailability(
                    tenantId,
                    staffId,
                    requested.DayOfWeek,
                    requested.StartTime,
                    requested.EndTime);

            _database.StaffAvailability.Add(
                availability);
        }

        await _database.SaveChangesAsync(
            cancellationToken);

        return await GetAsync(
            staffId,
            cancellationToken);
    }

    private async Task EnsureStaffExistsAsync(
        Guid tenantId,
        Guid staffId,
        CancellationToken cancellationToken)
    {
        var exists =
            await _database.StaffMembers
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == staffId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Staff member was not found.");
        }
    }

    private static void ValidateRequest(
        SaveStaffAvailabilityRequest request)
    {
        if (request.Days is null)
        {
            throw new InvalidOperationException(
                "Working days are required.");
        }

        var duplicateDay =
            request.Days
                .GroupBy(x => x.DayOfWeek)
                .FirstOrDefault(x =>
                    x.Count() > 1);

        if (duplicateDay is not null)
        {
            throw new InvalidOperationException(
                $"Only one working period may be entered for {duplicateDay.Key}.");
        }

        foreach (var day in request.Days)
        {
            if (day.StartTime >= day.EndTime)
            {
                throw new InvalidOperationException(
                    $"{day.DayOfWeek}: start time must be earlier than end time.");
            }
        }
    }

    private static int SortDay(
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
}
