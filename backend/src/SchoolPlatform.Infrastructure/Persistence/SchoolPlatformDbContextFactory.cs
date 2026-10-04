using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolPlatform.Infrastructure.Persistence;

public sealed class SchoolPlatformDbContextFactory : IDesignTimeDbContextFactory<SchoolPlatformDbContext>
{
    public SchoolPlatformDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SCHOOL_PLATFORM_DESIGN_CONNECTION")
            ?? "Host=127.0.0.1;Port=5433;Database=schoolplatform;Username=schoolplatform;Password=schoolplatform_dev_password";
        var options = new DbContextOptionsBuilder<SchoolPlatformDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new SchoolPlatformDbContext(options);
    }
}
