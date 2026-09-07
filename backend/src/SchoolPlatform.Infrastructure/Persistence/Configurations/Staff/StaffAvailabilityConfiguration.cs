using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Staff;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Staff;

public sealed class StaffAvailabilityConfiguration
    : IEntityTypeConfiguration<StaffAvailability>
{
    public void Configure(
        EntityTypeBuilder<StaffAvailability> builder)
    {
        builder.ToTable("staff_availability");

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
            x.StaffMemberId,
            x.DayOfWeek
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.IsActive
        });

        builder.HasOne(x => x.StaffMember)
            .WithMany(x => x.Availability)
            .HasForeignKey(x => x.StaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
