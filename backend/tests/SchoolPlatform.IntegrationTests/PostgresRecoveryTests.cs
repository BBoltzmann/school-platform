using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCHOOL_AUTH_TEST_POSTGRES_SOCKET") is null)
            Skip = "Run with an isolated local PostgreSQL socket to verify PostgreSQL transaction semantics.";
    }
}

public sealed class ResetReadBarrier : DbCommandInterceptor
{
    public bool Enabled { get; set; }
    private int readers;
    private readonly TaskCompletionSource bothReading = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (Enabled && command.CommandText.Contains("FROM password_reset_tokens", StringComparison.Ordinal)
            && command.CommandText.StartsWith("SELECT", StringComparison.Ordinal))
        {
            if (Interlocked.Increment(ref readers) == 2) bothReading.TrySetResult();
            await bothReading.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
        return result;
    }
}

public sealed class PostgresRecoveryTests
{
    [PostgresFact]
    public Task SameTokenCannotBeConsumedByConcurrentRequests() => AssertConcurrentReset(false);

    [PostgresFact]
    public Task DifferentOutstandingTokensCannotBothResetTheSameUserConcurrently() => AssertConcurrentReset(true);

    private static async Task AssertConcurrentReset(bool differentTokens)
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        var request = new ForgotPasswordRequest(AuthenticationFactory.AdminEmail, "antioch-college");
        await client.PostAsJsonAsync("/api/auth/forgot-password", request);
        await factory.DeliverNextAsync();
        var first = factory.Email.LatestToken;
        if (differentTokens)
        {
            factory.Clock.Advance(TimeSpan.FromMinutes(2));
            await client.PostAsJsonAsync("/api/auth/forgot-password", request);
            await factory.DeliverNextAsync();
        }
        var second = factory.Email.LatestToken;
        factory.ResetBarrier.Enabled = true;
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(first, AuthenticationFactory.NewPassword)),
            client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(second, "Another-new-password-789")));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.BadRequest);
        factory.ResetBarrier.Enabled = false;
        using var scope = factory.Services.CreateScope();
        Assert.All(await scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>().PasswordResetTokens.ToListAsync(),
            token => Assert.NotNull(token.UsedAtUtc));
    }

    [PostgresFact]
    public async Task MigrationPreservesExistingAntiochAndPasswordData()
    {
        using var factory = new AuthenticationFactory();
        using var client = await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        var tenantId = (await database.Tenants.SingleAsync()).Id;
        var passwordHash = (await database.Users.SingleAsync()).PasswordHash;
        var migrator = database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260907093519_AddFeesManagementCore");
        await migrator.MigrateAsync();
        database.ChangeTracker.Clear();
        Assert.Equal(tenantId, (await database.Tenants.SingleAsync()).Id);
        Assert.Equal(passwordHash, (await database.Users.SingleAsync()).PasswordHash);
        Assert.Single(await database.TenantMemberships.ToListAsync());
        Assert.Empty(await database.PasswordResetTokens.ToListAsync());
    }
}
