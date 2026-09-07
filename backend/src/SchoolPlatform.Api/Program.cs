using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolPlatform.Api.Security;
using SchoolPlatform.Application.Academics;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Academics;
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

builder.Services.AddScoped<
    IAcademicSetupService,
    AcademicSetupService>();


builder.Services.AddScoped<
    SchoolPlatform.Application.Students.IStudentService,
    SchoolPlatform.Infrastructure.Students.StudentService>();


builder.Services.AddScoped<
    SchoolPlatform.Application.Students.IGuardianService,
    SchoolPlatform.Infrastructure.Students.GuardianService>();

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

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<CurrentRequestContext>();

builder.Services.AddScoped<ICurrentUserContext>(
    serviceProvider =>
        serviceProvider.GetRequiredService<CurrentRequestContext>());

builder.Services.AddScoped<ITenantContext>(
    serviceProvider =>
        serviceProvider.GetRequiredService<CurrentRequestContext>());

builder.Services.AddScoped<
    SchoolPlatform.Application.Admissions.IAdmissionService,
    SchoolPlatform.Infrastructure.Admissions.AdmissionService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Admissions.IAdmissionDocumentService,
    SchoolPlatform.Infrastructure.Admissions.AdmissionDocumentService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Staff.IStaffService,
    SchoolPlatform.Infrastructure.Staff.StaffService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Staff.IStaffAvailabilityService,
    SchoolPlatform.Infrastructure.Staff.StaffAvailabilityService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Staff.ITeachingAssignmentService,
    SchoolPlatform.Infrastructure.Staff.TeachingAssignmentService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Timetabling.ITimetablePlanningService,
    SchoolPlatform.Infrastructure.Timetabling.TimetablePlanningService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Timetabling.ITimetableReadinessService,
    SchoolPlatform.Infrastructure.Timetabling.TimetableReadinessService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Timetabling.ITimetableGenerationService,
    SchoolPlatform.Infrastructure.Timetabling.TimetableGenerationService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Assessments.IAssessmentService,
    SchoolPlatform.Infrastructure.Assessments.AssessmentService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Inventory.IInventoryService,
    SchoolPlatform.Infrastructure.Inventory.InventoryService>();

builder.Services.AddScoped<
    SchoolPlatform.Application.Fees.IFeesService,
    SchoolPlatform.Infrastructure.Fees.FeesService>();

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

app.MapGet(
    "/health",
    async (
        SchoolPlatformDbContext database,
        CancellationToken cancellationToken) =>
    {
        var databaseAvailable =
            await database.Database.CanConnectAsync(
                cancellationToken);

        return databaseAvailable
            ? Results.Ok(new
            {
                status = "healthy",
                database = "connected"
            })
            : Results.Problem(
                title: "Database unavailable",
                statusCode:
                    StatusCodes.Status503ServiceUnavailable);
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

app.MapGet(
        "/api/tenant/context",
        (
            ICurrentUserContext currentUser,
            ITenantContext tenant) =>
        {
            return Results.Ok(new
            {
                currentUser.IsAuthenticated,
                currentUser.UserId,
                currentUser.Email,

                tenant.TenantId,
                tenant.TenantSlug,

                currentUser.MembershipId,
                currentUser.Roles,
                currentUser.Permissions
            });
        })
    .RequireAuthorization();

app.MapGet(
        "/api/academics/setup",
        async (
            IAcademicSetupService academicSetupService,
            ICurrentUserContext currentUser,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission(
                    "academics.configure"))
            {
                return Results.Forbid();
            }

            var result =
                await academicSetupService.GetSetupAsync(
                    cancellationToken);

            return Results.Ok(result);
        })
    .RequireAuthorization();

app.MapPost(
        "/api/academics/sessions",
        async (
            CreateAcademicSessionRequest request,
            IAcademicSetupService academicSetupService,
            ICurrentUserContext currentUser,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission(
                    "academics.configure"))
            {
                return Results.Forbid();
            }

            try
            {
                var result =
                    await academicSetupService.CreateSessionAsync(
                        request,
                        cancellationToken);

                return Results.Created(
                    $"/api/academics/sessions/{result.Id}",
                    result);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new
                {
                    error = exception.Message
                });
            }
        })
    .RequireAuthorization();

app.MapPost(
        "/api/academics/terms",
        async (
            CreateAcademicTermRequest request,
            IAcademicSetupService academicSetupService,
            ICurrentUserContext currentUser,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission(
                    "academics.configure"))
            {
                return Results.Forbid();
            }

            try
            {
                var result =
                    await academicSetupService.CreateTermAsync(
                        request,
                        cancellationToken);

                return Results.Created(
                    $"/api/academics/terms/{result.Id}",
                    result);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new
                {
                    error = exception.Message
                });
            }
        })
    .RequireAuthorization();

app.MapPost(
        "/api/academics/levels",
        async (
            CreateAcademicLevelRequest request,
            IAcademicSetupService academicSetupService,
            ICurrentUserContext currentUser,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission(
                    "academics.configure"))
            {
                return Results.Forbid();
            }

            try
            {
                var result =
                    await academicSetupService.CreateLevelAsync(
                        request,
                        cancellationToken);

                return Results.Created(
                    $"/api/academics/levels/{result.Id}",
                    result);
            }
            catch (DbUpdateException)
            {
                return Results.BadRequest(new
                {
                    error =
                        "Academic level already exists or is invalid."
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new
                {
                    error = exception.Message
                });
            }
        })
    .RequireAuthorization();

app.MapPost(
        "/api/academics/classes",
        async (
            CreateClassGroupRequest request,
            IAcademicSetupService academicSetupService,
            ICurrentUserContext currentUser,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission(
                    "academics.configure"))
            {
                return Results.Forbid();
            }

            try
            {
                var result =
                    await academicSetupService.CreateClassAsync(
                        request,
                        cancellationToken);

                return Results.Created(
                    $"/api/academics/classes/{result.Id}",
                    result);
            }
            catch (DbUpdateException)
            {
                return Results.BadRequest(new
                {
                    error =
                        "Class already exists or is invalid."
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new
                {
                    error = exception.Message
                });
            }
        })
    .RequireAuthorization();

app.MapPost(
        "/api/academics/subjects",
        async (
            CreateSubjectRequest request,
            IAcademicSetupService academicSetupService,
            ICurrentUserContext currentUser,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission(
                    "academics.configure"))
            {
                return Results.Forbid();
            }

            try
            {
                var result =
                    await academicSetupService.CreateSubjectAsync(
                        request,
                        cancellationToken);

                return Results.Created(
                    $"/api/academics/subjects/{result.Id}",
                    result);
            }
            catch (DbUpdateException)
            {
                return Results.BadRequest(new
                {
                    error =
                        "Subject already exists or is invalid."
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new
                {
                    error = exception.Message
                });
            }
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

SchoolPlatform.Api.Endpoints.AcademicManagementEndpoints.MapAcademicManagementEndpoints(app);

SchoolPlatform.Api.Endpoints.StudentEndpoints.MapStudentEndpoints(app);

SchoolPlatform.Api.Endpoints.GuardianEndpoints.MapGuardianEndpoints(app);

SchoolPlatform.Api.Endpoints.AdmissionEndpoints.MapAdmissionEndpoints(app);

SchoolPlatform.Api.Endpoints.AdmissionDocumentEndpoints.MapAdmissionDocumentEndpoints(app);

SchoolPlatform.Api.Endpoints.StaffEndpoints.MapStaffEndpoints(app);

SchoolPlatform.Api.Endpoints.StaffAvailabilityEndpoints.MapStaffAvailabilityEndpoints(app);

SchoolPlatform.Api.Endpoints.TeachingAssignmentEndpoints.MapTeachingAssignmentEndpoints(app);

SchoolPlatform.Api.Endpoints.TimetablePlanningEndpoints.MapTimetablePlanningEndpoints(app);

SchoolPlatform.Api.Endpoints.TimetableReadinessEndpoints.MapTimetableReadinessEndpoints(app);

SchoolPlatform.Api.Endpoints.TimetableGenerationEndpoints.MapTimetableGenerationEndpoints(app);

SchoolPlatform.Api.Endpoints.TimetableTermEndpoints.MapTimetableTermEndpoints(app);

SchoolPlatform.Api.Endpoints.AssessmentEndpoints.MapAssessmentEndpoints(app);

SchoolPlatform.Api.Endpoints.AssessmentSetupEndpoints.MapAssessmentSetupEndpoints(app);

SchoolPlatform.Api.Endpoints.InventoryEndpoints.MapInventoryEndpoints(app);

SchoolPlatform.Api.Endpoints.FeesEndpoints.MapFeesEndpoints(app);

app.Run();

public sealed record InitialPasswordRequest(
    string Email,
    string Password);

public partial class Program
{
}
