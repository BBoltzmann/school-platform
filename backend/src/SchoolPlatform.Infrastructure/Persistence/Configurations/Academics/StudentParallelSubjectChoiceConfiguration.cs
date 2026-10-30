using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Academics;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Academics;

public sealed class StudentParallelSubjectChoiceConfiguration : IEntityTypeConfiguration<StudentParallelSubjectChoice>
{
    public void Configure(EntityTypeBuilder<StudentParallelSubjectChoice> builder)
    {
        builder.ToTable("student_parallel_subject_choices");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.StudentId, x.AcademicSessionId,
            x.ClassGroupId, x.ParallelSubjectGroupId }).IsUnique().HasDatabaseName("UX_student_parallel_choice_student_group");
        builder.HasIndex(x => new { x.TenantId, x.AcademicSessionId, x.ClassGroupId, x.SubjectId }).HasDatabaseName("IX_student_parallel_choice_class_subject");
    }
}
