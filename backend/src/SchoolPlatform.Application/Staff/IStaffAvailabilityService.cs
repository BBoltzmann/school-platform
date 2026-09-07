namespace SchoolPlatform.Application.Staff;

public interface IStaffAvailabilityService
{
    Task<IReadOnlyCollection<StaffAvailabilityResult>> GetAsync(
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StaffAvailabilityResult>> SaveAsync(
        Guid staffId,
        SaveStaffAvailabilityRequest request,
        CancellationToken cancellationToken = default);
}
