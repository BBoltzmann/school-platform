using SchoolPlatform.Domain.Common;
using SchoolPlatform.Domain.Students;

namespace SchoolPlatform.Domain.Fees;

public sealed class FeeStructureStudentAssignment : TenantEntity
{
    private FeeStructureStudentAssignment()
    {
    }

    public FeeStructureStudentAssignment(
        Guid tenantId,
        Guid feeStructureId,
        Guid studentId)
    {
        TenantId = tenantId;
        FeeStructureId = feeStructureId;
        StudentId = studentId;
        AssignedAtUtc = DateTime.UtcNow;
    }

    public Guid FeeStructureId { get; private set; }

    public Guid StudentId { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }

    public FeeStructure FeeStructure { get; private set; } = null!;

    public Student Student { get; private set; } = null!;
}
