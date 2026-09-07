namespace SchoolPlatform.Application.Academics;

public interface IAcademicSetupService
{
    Task<AcademicSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default);

    Task<AcademicSessionResult> CreateSessionAsync(
        CreateAcademicSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicTermResult> CreateTermAsync(
        CreateAcademicTermRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicLevelResult> CreateLevelAsync(
        CreateAcademicLevelRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassGroupResult> CreateClassAsync(
        CreateClassGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<SubjectResult> CreateSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicLevelResult> UpdateLevelAsync(
        Guid id,
        UpdateAcademicLevelRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicLevelResult> SetLevelStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<ClassGroupResult> UpdateClassAsync(
        Guid id,
        UpdateClassGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassGroupResult> SetClassStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<SubjectResult> UpdateSubjectAsync(
        Guid id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken = default);

    Task<SubjectResult> SetSubjectStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
