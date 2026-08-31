using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

public sealed class Permission : Entity
{
    private Permission()
    {
    }

    public Permission(string code, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code is required.", nameof(code));

        Code = code.Trim().ToLowerInvariant();
        Description = description?.Trim();
    }

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public ICollection<RolePermission> Roles { get; private set; }
        = new List<RolePermission>();
}
