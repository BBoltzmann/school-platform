namespace SchoolPlatform.Application.Email;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, Uri resetUrl, CancellationToken cancellationToken = default);
}
