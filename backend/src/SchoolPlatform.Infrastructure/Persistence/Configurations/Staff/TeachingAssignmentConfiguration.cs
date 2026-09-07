using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Staff;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Staff;

public sealed class TeachingAssignmentConfiguration
    : IEntityTypeConfiguration<TeachingAssignment>
{
    public void Configure(
        EntityTypeBuilder<TeachingAssignment> builder)
    {
        builder.ToTable("teaching_assignments");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StaffMemberId,
            x.AcademicSessionId,
            x.ClassGroupId,
            x.SubjectId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId,
            x.ClassGroupId,
            x.SubjectId,
            x.IsActive
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StaffMemberId,
            x.IsActive
        });

        builder.HasOne(x => x.StaffMember)
            .WithMany(x => x.TeachingAssignments)
            .HasForeignKey(x => x.StaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AcademicSession)
            .WithMany()
            .HasForeignKey(x => x.AcademicSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClassGroup)
            .WithMany()
            .HasForeignKey(x => x.ClassGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
