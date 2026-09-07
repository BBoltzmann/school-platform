using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Timetabling;

public sealed class TimetableSettingsConfiguration
    : IEntityTypeConfiguration<TimetableSettings>
{
    public void Configure(
        EntityTypeBuilder<TimetableSettings> builder)
    {
        builder.ToTable(
            "timetable_settings");

        builder.HasKey(x => x.Id);

        builder.Property(
                x => x.PeriodDurationMinutes)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId
        })
        .IsUnique();

        builder.HasOne(x =>
                x.AcademicSession)
            .WithMany()
            .HasForeignKey(x =>
                x.AcademicSessionId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
