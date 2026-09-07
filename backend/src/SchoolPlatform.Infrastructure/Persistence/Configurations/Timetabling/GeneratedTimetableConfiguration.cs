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

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicTermId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId
        });
    }
}
