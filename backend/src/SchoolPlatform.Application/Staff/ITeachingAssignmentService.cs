namespace SchoolPlatform.Application.Staff;

public interface ITeachingAssignmentService
{
    Task<TeachingAssignmentSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TeachingAssignmentResult>> GetForStaffAsync(
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<TeachingAssignmentResult> CreateAsync(
        CreateTeachingAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default);
}
