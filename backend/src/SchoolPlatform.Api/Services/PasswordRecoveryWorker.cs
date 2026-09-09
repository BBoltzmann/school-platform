using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Api.Services;

public sealed class PasswordRecoveryWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<PasswordRecoveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch
            {
                // Exception text can contain provider details; never log it or message payloads.
                logger.LogError("Password recovery delivery failed. Check database migrations, PASSWORD_RESET_BASE_URL, and email provider configuration.");
            }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    public async Task ProcessNextAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var now = clock.GetUtcNow().UtcDateTime;
        var job = await database.PasswordRecoveryJobs.AsNoTracking()
            .Where(x => x.NextAttemptAtUtc <= now).OrderBy(x => x.NextAttemptAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null) return;
        // An atomic lease prevents multiple replicas processing the same request concurrently.
        var claimed = await database.PasswordRecoveryJobs
            .Where(x => x.Id == job.Id && x.NextAttemptAtUtc <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAtUtc, now.AddMinutes(2))
                .SetProperty(x => x.Attempts, x => x.Attempts + 1), cancellationToken);
        if (claimed == 0) return;
        if (job.Attempts >= 5 || job.CreatedAtUtc < now.AddHours(-24))
        {
            await database.PasswordRecoveryJobs.Where(x => x.Id == job.Id).ExecuteDeleteAsync(cancellationToken);
            logger.LogError("A password recovery delivery exhausted its retries. Check email provider configuration and delivery health.");
            return;
        }
        await scope.ServiceProvider.GetRequiredService<IPasswordRecoveryService>()
            .RequestAsync(new ForgotPasswordRequest(job.Email, job.TenantSlug), cancellationToken);
        await database.PasswordRecoveryJobs.Where(x => x.Id == job.Id).ExecuteDeleteAsync(cancellationToken);
        // Retain no token history beyond its useful lifetime.
        await database.PasswordResetTokens.Where(x => x.ExpiresAtUtc < now.AddDays(-1))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
