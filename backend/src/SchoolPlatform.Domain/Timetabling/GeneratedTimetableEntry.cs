using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Timetabling;

public sealed class GeneratedTimetableEntry : TenantEntity
{
    private GeneratedTimetableEntry()
    {
    }

    public GeneratedTimetableEntry(
        Guid tenantId,
        Guid generatedTimetableId,
        Guid classGroupId,
        Guid subjectId,
        Guid staffMemberId,
        DayOfWeek dayOfWeek,
        int periodNumber,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        TenantId = tenantId;
        GeneratedTimetableId = generatedTimetableId;
        ClassGroupId = classGroupId;
        SubjectId = subjectId;
        StaffMemberId = staffMemberId;
        DayOfWeek = dayOfWeek;
        PeriodNumber = periodNumber;
        StartTime = startTime;
        EndTime = endTime;
    }

    public Guid GeneratedTimetableId { get; private set; }

    public Guid ClassGroupId { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid StaffMemberId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public int PeriodNumber { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public GeneratedTimetable GeneratedTimetable { get; private set; } = null!;
}
