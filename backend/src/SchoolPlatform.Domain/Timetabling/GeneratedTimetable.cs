using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Timetabling;

public sealed class GeneratedTimetable : TenantEntity
{
    private GeneratedTimetable()
    {
    }

    public GeneratedTimetable(
        Guid tenantId,
        Guid academicSessionId,
        Guid academicTermId)
    {
        TenantId = tenantId;
        AcademicSessionId = academicSessionId;
        AcademicTermId = academicTermId;
        GeneratedAtUtc = DateTime.UtcNow;
    }

    public Guid AcademicSessionId { get; private set; }

    public Guid AcademicTermId { get; private set; }

    public DateTime GeneratedAtUtc { get; private set; }

    public ICollection<GeneratedTimetableEntry> Entries { get; private set; }
        = new List<GeneratedTimetableEntry>();

    public void MarkRegenerated()
    {
        GeneratedAtUtc = DateTime.UtcNow;
    }
}
