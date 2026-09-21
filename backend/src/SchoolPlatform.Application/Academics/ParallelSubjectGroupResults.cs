namespace SchoolPlatform.Application.Academics;

public sealed record ParallelSubjectGroupMemberResult(
    Guid ClassSubjectId,
    Guid SubjectId,
    string SubjectName,
    string SubjectCode,
    int PeriodsPerWeek,
    string? TeacherName);

public sealed record ParallelSubjectGroupResult(
    Guid Id,
    Guid AcademicSessionId,
    Guid ClassGroupId,
    string? DisplayName,
    bool IsActive,
    int SharedPeriodsPerWeek,
    IReadOnlyCollection<ParallelSubjectGroupMemberResult> Members);

public sealed record SaveParallelSubjectGroupRequest(
    Guid AcademicSessionId,
    string? DisplayName,
    IReadOnlyCollection<Guid> ClassSubjectIds);

public interface IParallelSubjectGroupService
{
    Task<IReadOnlyCollection<ParallelSubjectGroupResult>> ListAsync(Guid classGroupId, Guid academicSessionId, CancellationToken cancellationToken = default);
    Task<ParallelSubjectGroupResult> CreateAsync(Guid classGroupId, SaveParallelSubjectGroupRequest request, CancellationToken cancellationToken = default);
    Task<ParallelSubjectGroupResult> UpdateAsync(Guid classGroupId, Guid groupId, SaveParallelSubjectGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid classGroupId, Guid groupId, CancellationToken cancellationToken = default);
}
