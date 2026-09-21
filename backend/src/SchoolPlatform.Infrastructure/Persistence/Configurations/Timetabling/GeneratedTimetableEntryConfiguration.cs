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

        builder.HasOne(x => x.ClassGroup).WithMany().HasForeignKey(x => x.ClassGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);


        builder.Property(x => x.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.GeneratedTimetableId, x.ClassGroupId, x.DayOfWeek, x.PeriodNumber })
            .HasFilter("\"ParallelOccurrenceId\" IS NULL")
            .IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.GeneratedTimetableId, x.ClassGroupId, x.DayOfWeek, x.PeriodNumber, x.ParallelOccurrenceId })
            .HasFilter("\"ParallelOccurrenceId\" IS NOT NULL")
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

        builder.HasOne(x => x.ParallelSubjectGroup).WithMany().HasForeignKey(x => x.ParallelSubjectGroupId).OnDelete(DeleteBehavior.Restrict);
    }
}
