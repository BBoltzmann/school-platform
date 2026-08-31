using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Infrastructure.Persistence;
using SchoolPlatform.Infrastructure.Platform;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<SchoolPlatformDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<
    ISchoolBootstrapService,
    SchoolBootstrapService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

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
}

app.Run();

public partial class Program
{
}
