using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Email;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Authentication;

public sealed class PasswordRecoveryService(
    SchoolPlatformDbContext database,
    IPasswordHasher<User> passwordHasher,
    IEmailSender emailSender,
    IConfiguration configuration,
    TimeProvider clock) : IPasswordRecoveryService
{
    public async Task RequestAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var slug = request.TenantSlug.Trim().ToLowerInvariant();
        var identity = await (
            from user in database.Users
            join membership in database.TenantMemberships on user.Id equals membership.UserId
            join tenant in database.Tenants on membership.TenantId equals tenant.Id
            where user.Email == email && user.IsActive && membership.IsActive
                && tenant.Slug == slug && tenant.IsActive && user.PasswordHash != null
            select new { User = user, TenantId = tenant.Id }
        ).SingleOrDefaultAsync(cancellationToken);
        if (identity is null) return;

        // Trusted configuration only: never construct links from request Host/Origin headers.
        if (!Uri.TryCreate(configuration["PASSWORD_RESET_BASE_URL"], UriKind.Absolute, out var baseUrl)
            || (baseUrl.Scheme != "https" && !(baseUrl.Scheme == "http" && baseUrl.IsLoopback))
            || baseUrl.AbsolutePath != "/" || !string.IsNullOrEmpty(baseUrl.Query)
            || !string.IsNullOrEmpty(baseUrl.Fragment) || !string.IsNullOrEmpty(baseUrl.UserInfo))
            throw new InvalidOperationException("Password recovery URL configuration is missing or invalid.");

        var now = clock.GetUtcNow().UtcDateTime;
        // Bound mail volume for a known user without affecting the public response.
        if (await database.PasswordResetTokens.AnyAsync(x => x.UserId == identity.User.Id
            && x.UsedAtUtc == null && x.CreatedAtUtc > now.AddMinutes(-1), cancellationToken)) return;

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var reset = new PasswordResetToken(identity.User.Id, identity.TenantId, Hash(rawToken), now.AddMinutes(30));
        database.PasswordResetTokens.Add(reset);
        await database.SaveChangesAsync(cancellationToken);
        try
        {
            await emailSender.SendPasswordResetAsync(identity.User.Email,
                new Uri(baseUrl, $"reset-password?token={rawToken}"), cancellationToken);
        }
        catch
        {
            // A failed delivery must not leave a usable token or block the next attempt.
            await database.PasswordResetTokens.Where(x => x.Id == reset.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, now), CancellationToken.None);
            throw;
        }
    }

    public async Task<string?> ResetAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (!PasswordPolicy.IsValid(request.NewPassword) || request.Token is not { Length: 64 }
            || !request.Token.All(char.IsAsciiHexDigit)) return null;
        var hash = Hash(request.Token);
        var now = clock.GetUtcNow().UtcDateTime;
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var token = await database.PasswordResetTokens.AsNoTracking().SingleOrDefaultAsync(
                x => x.TokenHash == hash && x.UsedAtUtc == null && x.ExpiresAtUtc > now, cancellationToken);
            if (token is null) return null;
            var user = await database.Users.SingleOrDefaultAsync(x => x.Id == token.UserId && x.IsActive, cancellationToken);
            var slug = await (from tenant in database.Tenants
                join membership in database.TenantMemberships on tenant.Id equals membership.TenantId
                where tenant.Id == token.TenantId && tenant.IsActive && membership.IsActive && membership.UserId == token.UserId
                select tenant.Slug).SingleOrDefaultAsync(cancellationToken);
            if (user is null || slug is null) return null;

            // All tokens for this global identity are consumed in the same transaction.
            // Concurrent resets serialize on these rows; a losing transaction rolls back.
            await database.PasswordResetTokens.Where(x => x.UserId == user.Id && x.UsedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, now), cancellationToken);
            user.SetPasswordHash(passwordHasher.HashPassword(user, request.NewPassword));
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return slug;
        }
        catch (Exception exception) when (exception is PostgresException { SqlState: "40001" or "40P01" }
            || exception.InnerException is PostgresException { SqlState: "40001" or "40P01" })
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
