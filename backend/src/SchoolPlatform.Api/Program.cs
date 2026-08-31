using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Authentication;
using SchoolPlatform.Infrastructure.Persistence;
using SchoolPlatform.Infrastructure.Platform;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<SchoolPlatformDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions =
    builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration was not found.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "JWT signing key was not configured.");
}

builder.Services.AddScoped<
    ISchoolBootstrapService,
    SchoolBootstrapService>();

builder.Services.AddScoped<
    IAuthenticationService,
    AuthenticationService>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "SchoolPlatform.Api",
    status = "running"
}));

app.MapGet("/health", async (
    SchoolPlatformDbContext database,
    CancellationToken cancellationToken) =>
{
    var databaseAvailable =
        await database.Database.CanConnectAsync(cancellationToken);

    return databaseAvailable
        ? Results.Ok(new
        {
            status = "healthy",
            database = "connected"
        })
        : Results.Problem(
            title: "Database unavailable",
            statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapPost(
    "/api/auth/login",
    async (
        LoginRequest request,
        IAuthenticationService authenticationService,
        CancellationToken cancellationToken) =>
    {
        var result =
            await authenticationService.LoginAsync(
                request,
                cancellationToken);

        return result is null
            ? Results.Unauthorized()
            : Results.Ok(result);
    });

app.MapGet(
    "/api/auth/me",
    (HttpContext context) =>
    {
        var claims = context.User.Claims
            .Select(x => new
            {
                x.Type,
                x.Value
            });

        return Results.Ok(claims);
    })
    .RequireAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapPost(
        "/api/platform/bootstrap-school",
        async (
            BootstrapSchoolRequest request,
            ISchoolBootstrapService bootstrapService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result =
                    await bootstrapService.BootstrapAsync(
                        request,
                        cancellationToken);

                return Results.Created(
                    $"/api/platform/tenants/{result.TenantId}",
                    result);
            }
            catch (InvalidOperationException exception)
            {
                return Results.Conflict(new
                {
                    error = exception.Message
                });
            }
        });

    app.MapPost(
        "/api/platform/set-initial-password",
        async (
            InitialPasswordRequest request,
            IAuthenticationService authenticationService,
            CancellationToken cancellationToken) =>
        {
            var success =
                await authenticationService.SetInitialPasswordAsync(
                    request.Email,
                    request.Password,
                    cancellationToken);

            return success
                ? Results.NoContent()
                : Results.BadRequest(new
                {
                    error =
                        "Unable to set initial password."
                });
        });
}

app.Run();

public sealed record InitialPasswordRequest(
    string Email,
    string Password);

public partial class Program
{
}
