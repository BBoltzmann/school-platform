namespace SchoolPlatform.Application.Staff;

public interface IStaffService
{
    Task<IReadOnlyCollection<StaffMemberResult>> GetStaffAsync(
        CancellationToken cancellationToken = default);

    Task<StaffMemberResult?> GetStaffMemberAsync(
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<StaffMemberResult> CreateAsync(
        CreateStaffRequest request,
        CancellationToken cancellationToken = default);

    Task<StaffMemberResult> UpdateAsync(
        Guid staffId,
        UpdateStaffRequest request,
        CancellationToken cancellationToken = default);
}
