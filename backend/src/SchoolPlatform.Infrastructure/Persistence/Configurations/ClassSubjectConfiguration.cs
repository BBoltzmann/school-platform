using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Academics;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class ClassSubjectConfiguration : IEntityTypeConfiguration<ClassSubject>
{
    public void Configure(EntityTypeBuilder<ClassSubject> builder)
    {
        builder.ToTable("class_subjects");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ClassGroupId, x.SubjectId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.ClassGroupId });
        builder.HasIndex(x => new { x.TenantId, x.ClassGroupId, x.IsActive });
        builder.HasOne(x => x.ClassGroup).WithMany(x => x.ClassSubjects)
            .HasForeignKey(x => x.ClassGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Subject).WithMany()
            .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
    }
}
