using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Fees;

public sealed class FeeStructure : TenantEntity
{
    private FeeStructure()
    {
    }

    public FeeStructure(
        Guid tenantId,
        Guid academicSessionId,
        Guid academicTermId,
        string name,
        string audienceType,
        Guid? audienceId)
    {
        TenantId = tenantId;
        AcademicSessionId = academicSessionId;
        AcademicTermId = academicTermId;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Fee structure name is required.");
        }

        var allowed =
            new[]
            {
                "School",
                "Level",
                "Class"
            };

        var audience =
            allowed.FirstOrDefault(x =>
                string.Equals(
                    x,
                    audienceType,
                    StringComparison.OrdinalIgnoreCase));

        if (audience is null)
        {
            throw new ArgumentException(
                "Audience must be School, Level or Class.");
        }

        if (audience != "School" &&
            !audienceId.HasValue)
        {
            throw new ArgumentException(
                "A level or class must be selected.");
        }

        Name = name.Trim();
        AudienceType = audience;
        AudienceId =
            audience == "School"
                ? null
                : audienceId;

        IsActive = true;
    }

    public Guid AcademicSessionId { get; private set; }

    public Guid AcademicTermId { get; private set; }

    public string Name { get; private set; } = "";

    public string AudienceType { get; private set; } = "";

    public Guid? AudienceId { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<FeeStructureLine> Lines { get; private set; }
        = new List<FeeStructureLine>();

    public void UpdateDetails(
        string name,
        string audienceType,
        Guid? audienceId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Fee structure name is required.");
        }

        var audience = new[] { "School", "Level", "Class" }
            .FirstOrDefault(x => string.Equals(
                x,
                audienceType,
                StringComparison.OrdinalIgnoreCase));

        if (audience is null)
        {
            throw new ArgumentException("Audience must be School, Level or Class.");
        }

        if (audience != "School" && !audienceId.HasValue)
        {
            throw new ArgumentException("A level or class must be selected.");
        }

        Name = name.Trim();
        AudienceType = audience;
        AudienceId = audience == "School" ? null : audienceId;
    }
}
