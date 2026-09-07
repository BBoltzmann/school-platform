using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Timetabling;

public sealed class TimetableNonTeachingBlock : TenantEntity
{
    private TimetableNonTeachingBlock()
    {
    }

    public TimetableNonTeachingBlock(
        Guid tenantId,
        Guid timetableDayId,
        string name,
        TimeOnly startTime,
        TimeOnly endTime,
        int sortOrder)
    {
        Validate(
            name,
            startTime,
            endTime);

        TenantId = tenantId;
        TimetableDayId = timetableDayId;
        Name = name.Trim();
        StartTime = startTime;
        EndTime = endTime;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public Guid TimetableDayId { get; private set; }

    public string Name { get; private set; } = null!;

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public TimetableDay TimetableDay { get; private set; } = null!;

    public void Update(
        string name,
        TimeOnly startTime,
        TimeOnly endTime,
        int sortOrder)
    {
        Validate(
            name,
            startTime,
            endTime);

        Name = name.Trim();
        StartTime = startTime;
        EndTime = endTime;
        SortOrder = sortOrder;
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

    private static void Validate(
        string name,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Block name is required.");
        }

        if (startTime >= endTime)
        {
            throw new ArgumentException(
                "Block start time must be earlier than end time.");
        }
    }
}
