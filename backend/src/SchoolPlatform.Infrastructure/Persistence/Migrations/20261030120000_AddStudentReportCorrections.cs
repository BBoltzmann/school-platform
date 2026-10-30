using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolPlatform.Infrastructure.Persistence;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SchoolPlatformDbContext))]
[Migration("20261030120000_AddStudentReportCorrections")]
public sealed class AddStudentReportCorrections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "IsVoided", table: "academic_assessment_scores",
            type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime>(name: "VoidedAtUtc", table: "academic_assessment_scores",
            type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "VoidedByUserId", table: "academic_assessment_scores",
            type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "VoidReason", table: "academic_assessment_scores",
            type: "character varying(500)", maxLength: 500, nullable: true);

        migrationBuilder.CreateTable(
            name: "student_parallel_subject_choices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                ParallelSubjectGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_student_parallel_subject_choices", x => x.Id));
        migrationBuilder.CreateIndex(name: "UX_student_parallel_choice_student_group",
            table: "student_parallel_subject_choices",
            columns: new[] { "TenantId", "StudentId", "AcademicSessionId", "ClassGroupId", "ParallelSubjectGroupId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_student_parallel_choice_class_subject",
            table: "student_parallel_subject_choices",
            columns: new[] { "TenantId", "AcademicSessionId", "ClassGroupId", "SubjectId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // A rollback would destroy subject choices and mark-correction audit metadata.
        // Revert application code instead; retain this additive schema.
        throw new InvalidOperationException("This migration is intentionally non-destructive; restore from a verified backup if reversal is essential.");
    }
}
