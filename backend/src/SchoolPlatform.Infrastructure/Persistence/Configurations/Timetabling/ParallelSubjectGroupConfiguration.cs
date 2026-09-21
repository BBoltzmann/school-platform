using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Timetabling;

public sealed class ParallelSubjectGroupConfiguration : IEntityTypeConfiguration<ParallelSubjectGroup>
{
    public void Configure(EntityTypeBuilder<ParallelSubjectGroup> builder)
    {
        builder.ToTable("parallel_subject_groups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.HasIndex(x => new { x.TenantId, x.AcademicSessionId, x.ClassGroupId, x.IsActive });
        builder.HasOne(x => x.AcademicSession).WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ClassGroup).WithMany().HasForeignKey(x => x.ClassGroupId).OnDelete(DeleteBehavior.Restrict);
    }
}
