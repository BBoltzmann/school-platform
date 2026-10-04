using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Tenancy;

public sealed class TenantProfile : Entity
{
    private TenantProfile() { }

    public TenantProfile(Guid tenantId)
    {
        TenantId = tenantId;
        PrimaryColor = "#F5D900";
        SecondaryColor = "#0B0B0B";
        AccentColor = "#FFF8C9";
    }

    public Guid TenantId { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Address { get; private set; }
    public string? Motto { get; private set; }
    public string? Mission { get; private set; }
    public string? Vision { get; private set; }
    public string? ShortAbout { get; private set; }
    public string? LogoDataUrl { get; private set; }
    public string? IconDataUrl { get; private set; }
    public string PrimaryColor { get; private set; } = "#F5D900";
    public string SecondaryColor { get; private set; } = "#0B0B0B";
    public string AccentColor { get; private set; } = "#FFF8C9";

    public Tenant Tenant { get; private set; } = null!;

    public void Update(
        string? websiteUrl, string? contactEmail, string? contactPhone, string? address,
        string? motto, string? mission, string? vision, string? shortAbout,
        string? logoDataUrl, string? iconDataUrl, string? primaryColor,
        string? secondaryColor, string? accentColor)
    {
        WebsiteUrl = Clean(websiteUrl); ContactEmail = Clean(contactEmail); ContactPhone = Clean(contactPhone); Address = Clean(address);
        Motto = Clean(motto); Mission = Clean(mission); Vision = Clean(vision); ShortAbout = Clean(shortAbout);
        if (logoDataUrl is not null) LogoDataUrl = Clean(logoDataUrl);
        if (iconDataUrl is not null) IconDataUrl = Clean(iconDataUrl);
        PrimaryColor = string.IsNullOrWhiteSpace(primaryColor) ? PrimaryColor : primaryColor.Trim();
        SecondaryColor = string.IsNullOrWhiteSpace(secondaryColor) ? SecondaryColor : secondaryColor.Trim();
        AccentColor = string.IsNullOrWhiteSpace(accentColor) ? AccentColor : accentColor.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
