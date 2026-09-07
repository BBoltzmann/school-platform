using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Timetabling;

public sealed class ClassSubjectRequirement : TenantEntity
{
    private ClassSubjectRequirement()
    {
    }

    public ClassSubjectRequirement(
        Guid tenantId,
        Guid academicSessionId,
        Guid classGroupId,
        Guid subjectId,
        int periodsPerWeek)
    {
        ValidatePeriods(
            periodsPerWeek);

        TenantId = tenantId;
        AcademicSessionId = academicSessionId;
        ClassGroupId = classGroupId;
        SubjectId = subjectId;
        PeriodsPerWeek = periodsPerWeek;
        IsActive = true;
    }

    public Guid AcademicSessionId { get; private set; }

    public Guid ClassGroupId { get; private set; }

    public Guid SubjectId { get; private set; }

    public int PeriodsPerWeek { get; private set; }

    public bool IsActive { get; private set; }

    public AcademicSession AcademicSession { get; private set; } = null!;

    public ClassGroup ClassGroup { get; private set; } = null!;

    public Subject Subject { get; private set; } = null!;

    public void Update(
        int periodsPerWeek)
    {
        ValidatePeriods(
            periodsPerWeek);

        PeriodsPerWeek = periodsPerWeek;
        IsActive = true;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void ValidatePeriods(
        int periodsPerWeek)
    {
        if (periodsPerWeek <= 0)
        {
            throw new ArgumentException(
                "Periods per week must be greater than zero.");
        }

        if (periodsPerWeek > 50)
        {
            throw new ArgumentException(
                "Periods per week cannot exceed 50.");
        }
    }
}
