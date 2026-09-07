using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Timetabling;

public sealed class TimetableDay : TenantEntity
{
    private TimetableDay()
    {
    }

    public TimetableDay(
        Guid tenantId,
        Guid timetableSettingsId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        ValidateTimes(
            startTime,
            endTime);

        TenantId = tenantId;
        TimetableSettingsId = timetableSettingsId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = true;
    }

    public Guid TimetableSettingsId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public bool IsActive { get; private set; }

    public TimetableSettings TimetableSettings { get; private set; } = null!;

    public ICollection<TimetableNonTeachingBlock> NonTeachingBlocks { get; private set; }
        = new List<TimetableNonTeachingBlock>();

    public void Update(
        TimeOnly startTime,
        TimeOnly endTime)
    {
        ValidateTimes(
            startTime,
            endTime);

        StartTime = startTime;
        EndTime = endTime;
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

    private static void ValidateTimes(
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException(
                "School day start time must be earlier than end time.");
        }
    }
}
