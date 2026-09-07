using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Staff;

public sealed class StaffAvailability : TenantEntity
{
    private StaffAvailability()
    {
    }

    public StaffAvailability(
        Guid tenantId,
        Guid staffMemberId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException(
                "Start time must be earlier than end time.");
        }

        TenantId = tenantId;
        StaffMemberId = staffMemberId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = true;
    }

    public Guid StaffMemberId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public bool IsActive { get; private set; }

    public StaffMember StaffMember { get; private set; } = null!;

    public void Update(
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException(
                "Start time must be earlier than end time.");
        }

        StartTime = startTime;
        EndTime = endTime;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
