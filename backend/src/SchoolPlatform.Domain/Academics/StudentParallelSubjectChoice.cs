using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Academics;

// Explicit student choice; never inferred from assessment marks.
public sealed class StudentParallelSubjectChoice : TenantEntity
{
    private StudentParallelSubjectChoice() { }
    public StudentParallelSubjectChoice(Guid tenantId, Guid studentId, Guid sessionId,
        Guid classGroupId, Guid parallelGroupId, Guid subjectId)
    {
        TenantId = tenantId;
        StudentId = studentId;
        AcademicSessionId = sessionId;
        ClassGroupId = classGroupId;
        ParallelSubjectGroupId = parallelGroupId;
        SubjectId = subjectId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
    public Guid StudentId { get; private set; }
    public Guid AcademicSessionId { get; private set; }
    public Guid ClassGroupId { get; private set; }
    public Guid ParallelSubjectGroupId { get; private set; }
    public Guid SubjectId { get; private set; }
    public void Select(Guid subjectId)
    {
        SubjectId = subjectId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
