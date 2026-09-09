using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

public sealed class PasswordResetToken : Entity
{
    private PasswordResetToken() { }
    public PasswordResetToken(Guid userId, Guid tenantId, string tokenHash, DateTime expiresAtUtc)
    {
        UserId = userId;
        TenantId = tenantId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
}
