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
        ActivatedAtUtc = GeneratedAtUtc;
        IsActive = true;
        VersionNumber = 1;
    }

    public Guid AcademicSessionId { get; private set; }

    public Guid AcademicTermId { get; private set; }

    public DateTime GeneratedAtUtc { get; private set; }
    public int VersionNumber { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }
    public DateTime? SupersededAtUtc { get; private set; }

    public ICollection<GeneratedTimetableEntry> Entries { get; private set; }
        = new List<GeneratedTimetableEntry>();

    public void MarkRegenerated()
    {
        GeneratedAtUtc = DateTime.UtcNow;
    }

    public void SetVersion(int versionNumber) => VersionNumber = versionNumber;
    public void Activate() { IsActive = true; ActivatedAtUtc = DateTime.UtcNow; SupersededAtUtc = null; }
    public void MarkHistorical() { IsActive = false; SupersededAtUtc = DateTime.UtcNow; }
}
