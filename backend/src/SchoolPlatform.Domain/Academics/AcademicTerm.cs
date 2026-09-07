using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Academics;

public sealed class AcademicTerm : TenantEntity
{
    private AcademicTerm()
    {
    }

    public AcademicTerm(
        Guid tenantId,
        Guid academicSessionId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        int sortOrder)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (academicSessionId == Guid.Empty)
            throw new ArgumentException(
                "Academic session ID is required.",
                nameof(academicSessionId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Term name is required.", nameof(name));

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        if (sortOrder <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order must be greater than zero.");

        TenantId = tenantId;
        AcademicSessionId = academicSessionId;
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        SortOrder = sortOrder;
    }

    public Guid AcademicSessionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; } = true;

    public AcademicSession AcademicSession { get; private set; } = null!;
}
