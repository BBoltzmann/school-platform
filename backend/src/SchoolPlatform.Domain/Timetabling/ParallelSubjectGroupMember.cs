using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Timetabling;

public sealed class ParallelSubjectGroupMember : TenantEntity
{
    private ParallelSubjectGroupMember() { }

    public ParallelSubjectGroupMember(Guid tenantId, Guid parallelSubjectGroupId, Guid classSubjectId)
    {
        TenantId = tenantId;
        ParallelSubjectGroupId = parallelSubjectGroupId;
        ClassSubjectId = classSubjectId;
    }

    public Guid ParallelSubjectGroupId { get; private set; }
    public Guid ClassSubjectId { get; private set; }
    public ParallelSubjectGroup ParallelSubjectGroup { get; private set; } = null!;
    public ClassSubject ClassSubject { get; private set; } = null!;
}
