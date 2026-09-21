using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Timetabling;

public sealed class ParallelSubjectGroupMemberConfiguration : IEntityTypeConfiguration<ParallelSubjectGroupMember>
{
    public void Configure(EntityTypeBuilder<ParallelSubjectGroupMember> builder)
    {
        builder.ToTable("parallel_subject_group_members");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.ParallelSubjectGroupId, x.ClassSubjectId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.ClassSubjectId });
        builder.HasOne(x => x.ParallelSubjectGroup).WithMany(x => x.Members).HasForeignKey(x => x.ParallelSubjectGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ClassSubject).WithMany().HasForeignKey(x => x.ClassSubjectId).OnDelete(DeleteBehavior.Restrict);
    }
}
