using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Timetabling;

public sealed class GeneratedTimetableConfiguration
    : IEntityTypeConfiguration<GeneratedTimetable>
{
    public void Configure(
        EntityTypeBuilder<GeneratedTimetable> builder)
    {
        builder.ToTable("generated_timetables");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TenantId, x.AcademicSessionId, x.AcademicTermId })
            .HasDatabaseName("IX_generated_timetables_active_scope")
            .HasFilter("\"IsActive\" = true")
            .IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.AcademicSessionId, x.AcademicTermId, x.VersionNumber }).HasDatabaseName("IX_generated_timetables_version_scope").IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId
        });
    }
}
