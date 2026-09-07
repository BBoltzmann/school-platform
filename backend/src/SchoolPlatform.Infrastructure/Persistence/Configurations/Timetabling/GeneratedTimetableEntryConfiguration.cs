using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Timetabling;

public sealed class GeneratedTimetableEntryConfiguration
    : IEntityTypeConfiguration<GeneratedTimetableEntry>
{
    public void Configure(
        EntityTypeBuilder<GeneratedTimetableEntry> builder)
    {
        builder.ToTable(
            "generated_timetable_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.GeneratedTimetableId,
            x.ClassGroupId,
            x.DayOfWeek,
            x.PeriodNumber
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.GeneratedTimetableId,
            x.StaffMemberId,
            x.DayOfWeek,
            x.PeriodNumber
        })
        .IsUnique();

        builder.HasOne(x =>
                x.GeneratedTimetable)
            .WithMany(x =>
                x.Entries)
            .HasForeignKey(x =>
                x.GeneratedTimetableId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
