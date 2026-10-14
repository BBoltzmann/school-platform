using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Authentication;
using SchoolPlatform.Infrastructure.Persistence;

return await OperatorProgram.RunAsync(args);

internal static class OperatorProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            PrintUsage();
            return args.Length == 0 ? 2 : 0;
        }

        if (!string.Equals(args[0], "reset-school-user-password", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Unknown command.");
            PrintUsage();
            return 2;
        }

        var options = Parse(args[1..]);
        if (options.Error is not null)
        {
            Console.Error.WriteLine(options.Error);
            return 2;
        }

        var builder = Host.CreateApplicationBuilder(args);
        var apiSettings = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../SchoolPlatform.Api/appsettings.json"));
        if (File.Exists(apiSettings))
            builder.Configuration.AddJsonFile(apiSettings, optional: true, reloadOnChange: false);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("ConnectionStrings:DefaultConnection is not configured.");
            return 2;
        }

        builder.Services.AddDbContext<SchoolPlatformDbContext>(o => o.UseNpgsql(connectionString));
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IOperatorPasswordResetService, OperatorPasswordResetService>();

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var reset = scope.ServiceProvider.GetRequiredService<IOperatorPasswordResetService>();

        var password = options.DryRun
            ? string.Empty
            : ReadPasswordFromEnvironmentOrPrompt();
        if (!options.DryRun && !PasswordPolicy.IsValid(password))
        {
            Console.Error.WriteLine(PasswordPolicy.Description);
            return 2;
        }

        var result = await reset.ResetAsync(new OperatorPasswordResetRequest(
            options.SchoolSlug!, options.Email!, password, options.DryRun));

        if (!result.Succeeded)
        {
            Console.Error.WriteLine(result.Message);
            return 1;
        }

        Console.WriteLine(options.DryRun ? "Dry run: school user resolved." : "School user password updated.");
        Console.WriteLine($"School: {result.SchoolName} ({result.SchoolSlug})");
        Console.WriteLine($"Email: {result.Email}");
        Console.WriteLine($"Account enabled: {(result.AccountEnabled ? "yes" : "no")}");
        Console.WriteLine($"Setup completed: {(result.SetupCompleted ? "yes" : "no")}");
        Console.WriteLine($"Password credential present: {(result.PasswordCredentialPresent ? "yes" : "no")}");
        Console.WriteLine($"Locked out: {(result.LockedOut ? "yes" : "no")}");
        if (result.Roles.Count > 0)
            Console.WriteLine($"Roles: {string.Join(", ", result.Roles)}");
        return 0;
    }

    private static string ReadPasswordFromEnvironmentOrPrompt()
    {
        var value = Environment.GetEnvironmentVariable("SCHOOL_OPERATOR_PASSWORD");
        if (!string.IsNullOrEmpty(value)) return value;

        Console.Write("New password: ");
        var chars = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Count > 0) chars.RemoveAt(chars.Count - 1);
                continue;
            }
            if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
        }
        Console.WriteLine();
        return new string(chars.ToArray());
    }

    private static ParsedOptions Parse(string[] args)
    {
        string? slug = null, email = null;
        var dryRun = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--school-slug" when i + 1 < args.Length: slug = args[++i]; break;
                case "--email" when i + 1 < args.Length: email = args[++i]; break;
                case "--dry-run": dryRun = true; break;
                default: return new(null, null, false, $"Unknown or incomplete option: {args[i]}");
            }
        }
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(email))
            return new(null, null, false, "Both --school-slug and --email are required.");
        return new(slug, email, dryRun, null);
    }

    private static void PrintUsage() =>
        Console.WriteLine("dotnet run --project backend/src/SchoolPlatform.Operator -- reset-school-user-password --school-slug <slug> --email <email> [--dry-run]\n\nSet SCHOOL_OPERATOR_PASSWORD to avoid an interactive prompt. The password is never printed.");

    private sealed record ParsedOptions(string? SchoolSlug, string? Email, bool DryRun, string? Error);
}
