using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

public sealed class PlatformRoleAssignment : Entity
{
    private PlatformRoleAssignment() { }

    public PlatformRoleAssignment(Guid userId, string role)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role is required.", nameof(role));
        UserId = userId;
        Role = role.Trim();
        IsActive = true;
    }

    public Guid UserId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public User User { get; private set; } = null!;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
