using System.Security.Cryptography;
using System.Text;
using SchoolPlatform.Application.Authentication;

namespace SchoolPlatform.Infrastructure.Authentication;

// TEMPORARY operator-controlled recovery. Disable entirely with TEMP_PASSWORD_RESET_ENABLED=false.
// The production constructor reads only environment variables, never appsettings or request headers.
public sealed class TemporaryPasswordResetAccess : ITemporaryPasswordResetAccess
{
    private readonly Func<string, string?> readEnvironment;
    public TemporaryPasswordResetAccess() : this(Environment.GetEnvironmentVariable) { }
    public TemporaryPasswordResetAccess(Func<string, string?> readEnvironment) => this.readEnvironment = readEnvironment;

    public bool Enabled => string.Equals(readEnvironment("TEMP_PASSWORD_RESET_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);

    public bool Verify(string? submitted)
    {
        var expected = readEnvironment("TEMP_PASSWORD_RESET_CODE");
        if (!Enabled || expected is not { Length: >= 32 and <= 256 } || string.IsNullOrWhiteSpace(expected)
            || submitted is null || submitted.Length > 256) return false;
        // Compare fixed-size digests, including for wrong-length submissions. Never log either input.
        var expectedDigest = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var submittedDigest = SHA256.HashData(Encoding.UTF8.GetBytes(submitted));
        return CryptographicOperations.FixedTimeEquals(expectedDigest, submittedDigest);
    }
}
