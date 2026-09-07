namespace SchoolPlatform.Application.Staff;

public sealed record StaffAvailabilityResult(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

public sealed record SaveStaffAvailabilityItemRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record SaveStaffAvailabilityRequest(
    IReadOnlyCollection<SaveStaffAvailabilityItemRequest> Days);
