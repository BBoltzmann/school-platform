namespace SchoolPlatform.Application.Authentication;

public sealed record OperatorPasswordResetRequest(
    string SchoolSlug,
    string Email,
    string NewPassword,
    bool DryRun = false);

public sealed record OperatorPasswordResetResult(
    bool Succeeded,
    bool DryRun,
    string? Message,
    string? SchoolName,
    string? SchoolSlug,
    string? Email,
    bool AccountEnabled,
    bool SetupCompleted,
    bool PasswordCredentialPresent,
    bool LockedOut,
    IReadOnlyList<string> Roles);

public interface IOperatorPasswordResetService
{
    Task<OperatorPasswordResetResult> ResetAsync(
        OperatorPasswordResetRequest request,
        CancellationToken cancellationToken = default);
}
