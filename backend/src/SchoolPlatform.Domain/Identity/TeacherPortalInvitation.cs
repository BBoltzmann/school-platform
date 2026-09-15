using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

public sealed class TeacherPortalInvitation : Entity
{
    private TeacherPortalInvitation() { }
    public TeacherPortalInvitation(Guid tenantId, Guid staffMemberId, string tokenHash, DateTime expiresAtUtc, Guid createdByUserId)
    {
        TenantId = tenantId; StaffMemberId = staffMemberId; TokenHash = tokenHash; ExpiresAtUtc = expiresAtUtc; CreatedByUserId = createdByUserId;
    }
    public Guid TenantId { get; private set; }
    public Guid StaffMemberId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public void Revoke(DateTime now) => RevokedAtUtc = now;
    public void Use(DateTime now) => UsedAtUtc = now;
}
