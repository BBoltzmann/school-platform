using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Academics;

public sealed class AcademicSession : TenantEntity
{
    private AcademicSession()
    {
    }

    public AcademicSession(
        Guid tenantId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        bool isCurrent = false)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Session name is required.", nameof(name));

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        TenantId = tenantId;
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
    }

    public string Name { get; private set; } = string.Empty;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public bool IsCurrent { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<AcademicTerm> Terms { get; private set; }
        = new List<AcademicTerm>();

    public void MakeCurrent()
    {
        IsCurrent = true;
    }

    public void RemoveCurrentStatus()
    {
        IsCurrent = false;
    }
}
