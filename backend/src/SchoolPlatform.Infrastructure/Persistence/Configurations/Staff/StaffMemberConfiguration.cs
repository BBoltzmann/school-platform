using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Staff;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Staff;

public sealed class StaffMemberConfiguration
    : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(
        EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("staff_members");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StaffNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MiddleName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.Phone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.JobTitle)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Department)
            .HasMaxLength(150);

        builder.Property(x => x.EmploymentType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StaffNumber
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.IsTeachingStaff,
            x.IsActive
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Department
        });
    }
}
