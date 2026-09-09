using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

// Durable delivery request. Raw reset tokens are never persisted in this queue.
public sealed class PasswordRecoveryJob : Entity
{
    private PasswordRecoveryJob() { }
    public PasswordRecoveryJob(string email, string tenantSlug, DateTime now)
    {
        Email = email;
        TenantSlug = tenantSlug;
        NextAttemptAtUtc = now;
    }
    public string Email { get; private set; } = string.Empty;
    public string TenantSlug { get; private set; } = string.Empty;
    public DateTime NextAttemptAtUtc { get; private set; }
    public int Attempts { get; private set; }
}
