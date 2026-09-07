using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Students;

public sealed class StudentGuardian : TenantEntity
{
    private StudentGuardian()
    {
    }

    public StudentGuardian(
        Guid tenantId,
        Guid studentId,
        Guid guardianId,
        string relationship,
        bool isPrimaryContact,
        bool isEmergencyContact,
        bool canPickUpStudent,
        bool livesWithStudent)
    {
        TenantId = tenantId;

        StudentId = studentId;
        GuardianId = guardianId;

        Relationship = relationship.Trim();

        IsPrimaryContact = isPrimaryContact;
        IsEmergencyContact = isEmergencyContact;
        CanPickUpStudent = canPickUpStudent;
        LivesWithStudent = livesWithStudent;

        IsActive = true;
    }

    public Guid StudentId { get; private set; }

    public Guid GuardianId { get; private set; }

    public string Relationship { get; private set; } = null!;

    public bool IsPrimaryContact { get; private set; }

    public bool IsEmergencyContact { get; private set; }

    public bool CanPickUpStudent { get; private set; }

    public bool LivesWithStudent { get; private set; }

    public bool IsActive { get; private set; }

    public Student Student { get; private set; } = null!;

    public Guardian Guardian { get; private set; } = null!;

    public void UpdateRelationship(
        string relationship,
        bool isPrimaryContact,
        bool isEmergencyContact,
        bool canPickUpStudent,
        bool livesWithStudent)
    {
        Relationship = relationship.Trim();

        IsPrimaryContact = isPrimaryContact;
        IsEmergencyContact = isEmergencyContact;
        CanPickUpStudent = canPickUpStudent;
        LivesWithStudent = livesWithStudent;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
