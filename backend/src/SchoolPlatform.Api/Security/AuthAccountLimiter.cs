using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

namespace SchoolPlatform.Api.Security;

public sealed class AuthAccountLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<string> limiter = PartitionedRateLimiter.Create<string, string>(key =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = key.StartsWith("recovery:", StringComparison.Ordinal) ? 3 : 10,
            Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true,
        }));

    public bool TryAcquire(string operation, string? email, string? tenantSlug)
    {
        var identity = $"{email?.Trim().ToLowerInvariant()}\n{tenantSlug?.Trim().ToLowerInvariant()}";
        var key = operation + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        using var lease = limiter.AttemptAcquire(key);
        return lease.IsAcquired;
    }
    public void Dispose() => limiter.Dispose();
}
