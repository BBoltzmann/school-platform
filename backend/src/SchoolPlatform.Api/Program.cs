using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<SchoolPlatformDbContext>(options =>
    options.UseNpgsql(connectionString));

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

app.Run();

public partial class Program;
