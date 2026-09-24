using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Fees;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Fees;

public sealed class FeeStructureStudentAssignmentConfiguration
    : IEntityTypeConfiguration<FeeStructureStudentAssignment>
{
    public void Configure(
        EntityTypeBuilder<FeeStructureStudentAssignment> builder)
    {
        builder.ToTable("fee_structure_student_assignments");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.FeeStructureId,
            x.StudentId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.FeeStructureId
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StudentId
        });

        builder.HasOne(x => x.FeeStructure)
            .WithMany()
            .HasForeignKey(x => x.FeeStructureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
