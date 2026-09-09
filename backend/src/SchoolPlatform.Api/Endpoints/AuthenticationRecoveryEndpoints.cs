using SchoolPlatform.Api.Security;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Api.Endpoints;

public static class AuthenticationRecoveryEndpoints
{
    public const string RecoveryMessage = "If an account exists, a password reset link has been sent.";
    public const string SignupMessage = "If these details are eligible, your school has been created. Sign in with your school slug and password. If you cannot sign in, contact support or your school administrator.";

    public static void MapAuthenticationRecoveryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").RequireRateLimiting("auth");
        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, SchoolPlatformDbContext database,
            AuthAccountLimiter limiter, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (!limiter.TryAcquire("recovery", request.Email, request.TenantSlug))
                return Results.Json(new { error = "Too many attempts. Please try again later." }, statusCode: 429);
            // Validate shape only; no account lookup on the request path.
            if (AuthInput.EmailIsValid(request.Email) && AuthInput.SlugIsValid(request.TenantSlug?.Trim().ToLowerInvariant()))
            {
                database.PasswordRecoveryJobs.Add(new PasswordRecoveryJob(request.Email.Trim().ToLowerInvariant(),
                    request.TenantSlug!.Trim().ToLowerInvariant(), clock.GetUtcNow().UtcDateTime));
                await database.SaveChangesAsync(cancellationToken);
            }
            return Results.Ok(new { message = RecoveryMessage });
        });
        group.MapPost("/reset-password", async (ResetPasswordRequest request, IPasswordRecoveryService recovery,
            CancellationToken cancellationToken) =>
        {
            if (!PasswordPolicy.IsValid(request.NewPassword))
                return Results.BadRequest(new { error = PasswordPolicy.Description });
            var slug = await recovery.ResetAsync(request, cancellationToken);
            return slug is null
                ? Results.BadRequest(new { error = "This reset link is invalid or expired. Request a new link." })
                : Results.Ok(new { tenantSlug = slug });
        });
        group.MapPost("/signup", async (CreateSchoolRequest request, ISchoolSignupService signup,
            CancellationToken cancellationToken) =>
        {
            var outcome = await signup.CreateAsync(request, cancellationToken);
            return outcome switch
            {
                SchoolSignupOutcome.Disabled => Results.NotFound(),
                SchoolSignupOutcome.Invalid => Results.BadRequest(new { error = "Check the school details and password requirements." }),
                SchoolSignupOutcome.SlugUnavailable => Results.Conflict(new { error = "This school slug is unavailable. Choose a different slug." }),
                _ => Results.Ok(new { message = SignupMessage }),
            };
        });
    }
}
