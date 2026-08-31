using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Tenancy;

public sealed class Tenant : Entity
{
    private Tenant()
    {
    }

    public Tenant(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tenant slug is required.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public ICollection<Campus> Campuses { get; private set; } = new List<Campus>();
}
