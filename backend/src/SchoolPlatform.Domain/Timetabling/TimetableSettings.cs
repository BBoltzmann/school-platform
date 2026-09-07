using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Timetabling;

public sealed class TimetableSettings : TenantEntity
{
    private TimetableSettings()
    {
    }

    public TimetableSettings(
        Guid tenantId,
        Guid academicSessionId,
        int periodDurationMinutes)
    {
        if (periodDurationMinutes <= 0)
        {
            throw new ArgumentException(
                "Period duration must be greater than zero.");
        }

        TenantId = tenantId;
        AcademicSessionId = academicSessionId;
        PeriodDurationMinutes = periodDurationMinutes;
        IsActive = true;
    }

    public Guid AcademicSessionId { get; private set; }

    public int PeriodDurationMinutes { get; private set; }

    public bool IsActive { get; private set; }

    public AcademicSession AcademicSession { get; private set; } = null!;

    public ICollection<TimetableDay> Days { get; private set; }
        = new List<TimetableDay>();

    public void UpdatePeriodDuration(
        int periodDurationMinutes)
    {
        if (periodDurationMinutes <= 0)
        {
            throw new ArgumentException(
                "Period duration must be greater than zero.");
        }

        PeriodDurationMinutes = periodDurationMinutes;
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
