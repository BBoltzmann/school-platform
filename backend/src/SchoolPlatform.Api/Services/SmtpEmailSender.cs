using System.Net;
using System.Net.Mail;
using SchoolPlatform.Application.Email;

namespace SchoolPlatform.Api.Services;

public sealed class SmtpEmailSender(IWebHostEnvironment environment, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendPasswordResetAsync(string email, Uri resetUrl, CancellationToken cancellationToken = default)
    {
        // Provider credentials are read exclusively from environment variables.
        var provider = Environment.GetEnvironmentVariable("EMAIL_PROVIDER");
        var host = Environment.GetEnvironmentVariable("SMTP_HOST");
        var from = Environment.GetEnvironmentVariable("SMTP_FROM");
        var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        var useTls = !string.Equals(Environment.GetEnvironmentVariable("SMTP_ENABLE_TLS"), "false", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(provider, "smtp", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from)
            || (!environment.IsDevelopment() && (!useTls || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))))
        {
            logger.LogError("Password recovery email is not configured. Set EMAIL_PROVIDER=smtp and SMTP_HOST, SMTP_FROM, SMTP_USERNAME, SMTP_PASSWORD; production requires TLS.");
            throw new InvalidOperationException("Email provider is unavailable.");
        }
        var port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var configuredPort) ? configuredPort : 587;
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = useTls,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrEmpty(username) ? null : new NetworkCredential(username, password),
        };
        using var message = new MailMessage(from, email)
        {
            Subject = "Reset your School Platform password",
            Body = $"A password reset was requested for your School Platform account.\n\n{resetUrl.AbsoluteUri}\n\nThis link expires in 30 minutes and can be used once. If you did not request this, ignore this email.",
            IsBodyHtml = false,
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        await client.SendMailAsync(message, timeout.Token);
    }
}
