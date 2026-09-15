namespace SchoolPlatform.Application.Teacher;

public sealed record TeacherInviteResult(string InviteUrl, DateTime ExpiresAtUtc);
public sealed record TeacherInviteStatusResult(string Status, DateTime? ExpiresAtUtc, string? Email);
public sealed record TeacherInvitePreviewResult(string TenantName, string StaffName, DateTime ExpiresAtUtc);
public sealed record TeacherInviteActivationRequest(string Token, string Email, string Password);
public interface ITeacherInvitationService
{
    Task<TeacherInviteResult> GenerateAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task<TeacherInviteStatusResult> GetStatusAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task<TeacherInvitePreviewResult?> PreviewAsync(string token, CancellationToken cancellationToken = default);
    Task<string?> ActivateAsync(TeacherInviteActivationRequest request, CancellationToken cancellationToken = default);
}
