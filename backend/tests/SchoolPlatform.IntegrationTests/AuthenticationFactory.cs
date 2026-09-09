using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolPlatform.Api.Services;
using SchoolPlatform.Application.Email;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.IntegrationTests;

public sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan duration) => now += duration;
}

public sealed class TestEmailSender : IEmailSender
{
    public List<(string Email, Uri Url)> Messages { get; } = [];
    public bool Fail { get; set; }
    public Task SendPasswordResetAsync(string email, Uri resetUrl, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new InvalidOperationException("Test delivery failure");
        Messages.Add((email, resetUrl));
        return Task.CompletedTask;
    }
    public string LatestToken => Messages.Last().Url.Query.Split("token=")[1];
}

public sealed class AuthenticationFactory(bool allowSignup = true) : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly string? postgresSocket = Environment.GetEnvironmentVariable("SCHOOL_AUTH_TEST_POSTGRES_SOCKET");
    private readonly string databaseName = "school_auth_test_" + Guid.NewGuid().ToString("N");
    public ResetReadBarrier ResetBarrier { get; } = new();
    public TestClock Clock { get; } = new();
    public TestEmailSender Email { get; } = new();
    public const string OldPassword = "Original-password-123";
    public const string NewPassword = "Replacement-password-456";
    public const string AdminEmail = "admin@antiochcollege.local";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused_test_database",
            ["Jwt:Key"] = "test-only-signing-key-with-at-least-sixty-four-characters-0000000000",
            ["Jwt:Issuer"] = "test-issuer", ["Jwt:Audience"] = "test-audience",
            ["ALLOW_PUBLIC_SCHOOL_SIGNUP"] = allowSignup ? "true" : "false",
            ["PASSWORD_RESET_BASE_URL"] = "https://school.example",
        }));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<SchoolPlatformDbContext>();
            services.RemoveAll<DbContextOptions<SchoolPlatformDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SchoolPlatformDbContext>>();
            if (postgresSocket is null)
            {
                connection.Open();
                services.AddDbContext<SchoolPlatformDbContext>(options => options.UseSqlite(connection));
            }
            else
            {
                if (!postgresSocket.StartsWith("/private/tmp/school-platform-auth-", StringComparison.Ordinal))
                    throw new InvalidOperationException("Tests require an isolated temporary PostgreSQL socket.");
                services.AddDbContext<SchoolPlatformDbContext>(options => options.UseNpgsql(
                    new NpgsqlConnectionStringBuilder { Host = postgresSocket, Database = databaseName,
                        Username = "school_auth_test", Pooling = false }.ConnectionString).AddInterceptors(ResetBarrier));
            }
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);
            var worker = services.Single(x => x.ServiceType == typeof(IHostedService)
                && x.ImplementationType == typeof(PasswordRecoveryWorker));
            services.Remove(worker);
        });
    }

    public async Task<HttpClient> InitializeAsync(bool seed = true)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolPlatformDbContext>();
        if (postgresSocket is null) await database.Database.EnsureCreatedAsync();
        else await database.Database.MigrateAsync();
        if (seed) await scope.ServiceProvider.GetRequiredService<ISchoolBootstrapService>().BootstrapAsync(
            new BootstrapSchoolRequest("Antioch Royal College", "antioch-college", "Primary campus", AdminEmail, "School", "Admin", OldPassword));
        return client;
    }

    public Task DeliverNextAsync() => new PasswordRecoveryWorker(Services.GetRequiredService<IServiceScopeFactory>(), Clock,
        NullLogger<PasswordRecoveryWorker>.Instance).ProcessNextAsync(CancellationToken.None);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
            if (postgresSocket is not null && postgresSocket.StartsWith("/private/tmp/school-platform-auth-", StringComparison.Ordinal))
            {
                using var admin = new NpgsqlConnection(new NpgsqlConnectionStringBuilder {
                    Host = postgresSocket, Database = "postgres", Username = "school_auth_test", Pooling = false }.ConnectionString);
                admin.Open();
                // databaseName is generated internally and cannot contain SQL metacharacters.
                using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS {databaseName} WITH (FORCE)", admin);
                command.ExecuteNonQuery();
            }
        }
    }
}
