namespace SchoolPlatform.Application.Academics;

public sealed record ClassSubjectResult(Guid Id, Guid SubjectId, string SubjectName, string SubjectCode);
public sealed record ClassSubjectOfferingResult(Guid ClassGroupId, bool UsesCustomSubjectOffering, IReadOnlyCollection<ClassSubjectResult> Subjects);
public sealed record SetClassSubjectsRequest(IReadOnlyCollection<Guid> SubjectIds);

public interface IClassSubjectService
{
    Task<ClassSubjectOfferingResult> GetAsync(Guid classGroupId, CancellationToken cancellationToken = default);
    Task<ClassSubjectOfferingResult> SetAsync(Guid classGroupId, SetClassSubjectsRequest request, CancellationToken cancellationToken = default);
    Task<ClassSubjectOfferingResult> ResetAsync(Guid classGroupId, CancellationToken cancellationToken = default);
}
