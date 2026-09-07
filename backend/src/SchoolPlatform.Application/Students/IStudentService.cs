namespace SchoolPlatform.Application.Students;

public interface IStudentService
{
    Task<IReadOnlyCollection<StudentListItemResult>> GetStudentsAsync(
        CancellationToken cancellationToken = default);

    Task<StudentDetailResult?> GetStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<StudentSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default);

    Task<StudentListItemResult> CreateStudentAsync(
        CreateStudentRequest request,
        CancellationToken cancellationToken = default);

    Task<StudentDetailResult> UpdateStudentAsync(
        Guid studentId,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default);

    Task<StudentDetailResult> UpdateCurrentPlacementAsync(
        Guid studentId,
        UpdateStudentPlacementRequest request,
        CancellationToken cancellationToken = default);
}
