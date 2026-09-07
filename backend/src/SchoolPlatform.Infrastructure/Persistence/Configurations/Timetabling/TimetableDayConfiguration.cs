using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Timetabling;

public sealed class TimetableDayConfiguration
    : IEntityTypeConfiguration<TimetableDay>
{
    public void Configure(
        EntityTypeBuilder<TimetableDay> builder)
    {
        builder.ToTable(
            "timetable_days");

        builder.HasKey(x => x.Id);

        builder.Property(x =>
                x.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x =>
                x.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x =>
                x.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.TimetableSettingsId,
            x.DayOfWeek
        })
        .IsUnique();

        builder.HasOne(x =>
                x.TimetableSettings)
            .WithMany(x =>
                x.Days)
            .HasForeignKey(x =>
                x.TimetableSettingsId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
