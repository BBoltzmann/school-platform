namespace SchoolPlatform.Application.Authentication;

public sealed record ForgotPasswordRequest(string Email, string TenantSlug);
public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record DirectPasswordResetRequest(string Email, string TenantSlug, string RecoveryCode, string NewPassword);
public interface ITemporaryPasswordResetAccess
{
    bool Enabled { get; }
    bool Verify(string? submitted);
}

public interface IPasswordRecoveryService
{
    Task<string?> DirectResetAsync(DirectPasswordResetRequest request, CancellationToken cancellationToken = default);
    Task RequestAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<string?> ResetAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public static class PasswordPolicy
{
    public const string Description = "Use 12–128 characters, including a letter and a number.";
    public static bool IsValid(string? password) => password is { Length: >= 12 and <= 128 }
        && password.Any(char.IsAsciiLetter) && password.Any(char.IsAsciiDigit);
}
