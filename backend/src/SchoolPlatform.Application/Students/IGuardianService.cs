namespace SchoolPlatform.Application.Students;

public interface IGuardianService
{
    Task<IReadOnlyCollection<StudentGuardianResult>> GetStudentGuardiansAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GuardianResult>> SearchGuardiansAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<StudentGuardianResult> CreateAndLinkGuardianAsync(
        Guid studentId,
        CreateGuardianForStudentRequest request,
        CancellationToken cancellationToken = default);

    Task<StudentGuardianResult> LinkExistingGuardianAsync(
        Guid studentId,
        LinkExistingGuardianRequest request,
        CancellationToken cancellationToken = default);
}
