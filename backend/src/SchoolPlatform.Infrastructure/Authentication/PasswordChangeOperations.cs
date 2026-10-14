using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Authentication;

/// <summary>Shared password credential transition used by user-facing and operator resets.</summary>
internal static class PasswordChangeOperations
{
    public static async Task ChangeAsync(
        SchoolPlatformDbContext database,
        IPasswordHasher<User> passwordHasher,
        User user,
        string password,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await database.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.UsedAtUtc, now),
                cancellationToken);

        user.SetPasswordHash(passwordHasher.HashPassword(user, password));
        await database.SaveChangesAsync(cancellationToken);
    }
}
