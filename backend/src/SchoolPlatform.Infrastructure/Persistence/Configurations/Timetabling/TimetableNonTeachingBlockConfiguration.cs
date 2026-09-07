using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Timetabling;

public sealed class TimetableNonTeachingBlockConfiguration
    : IEntityTypeConfiguration<TimetableNonTeachingBlock>
{
    public void Configure(
        EntityTypeBuilder<TimetableNonTeachingBlock> builder)
    {
        builder.ToTable(
            "timetable_non_teaching_blocks");

        builder.HasKey(x => x.Id);

        builder.Property(x =>
                x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x =>
                x.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x =>
                x.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x =>
                x.SortOrder)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.TimetableDayId,
            x.IsActive
        });

        builder.HasOne(x =>
                x.TimetableDay)
            .WithMany(x =>
                x.NonTeachingBlocks)
            .HasForeignKey(x =>
                x.TimetableDayId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
