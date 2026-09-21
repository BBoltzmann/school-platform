using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations;

/// <summary>
/// Phase A schema foundation only. Existing rows are untouched and no groups
/// are inferred or backfilled. Runtime scheduling support is Phase B.
/// </summary>
public partial class AddParallelSubjectGroupFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "parallel_subject_groups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_parallel_subject_groups", x => x.Id);
                table.ForeignKey("FK_parallel_subject_groups_academic_sessions_AcademicSessionId", x => x.AcademicSessionId, "academic_sessions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_parallel_subject_groups_class_groups_ClassGroupId", x => x.ClassGroupId, "class_groups", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "parallel_subject_group_members",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                ParallelSubjectGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                ClassSubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_parallel_subject_group_members", x => x.Id);
                table.ForeignKey("FK_parallel_subject_group_members_class_subjects_ClassSubjectId", x => x.ClassSubjectId, "class_subjects", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_parallel_subject_group_members_parallel_subject_groups_ParallelSubjectGroupId", x => x.ParallelSubjectGroupId, "parallel_subject_groups", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_parallel_subject_groups_AcademicSessionId", table: "parallel_subject_groups", column: "AcademicSessionId");
        migrationBuilder.CreateIndex(name: "IX_parallel_subject_groups_ClassGroupId", table: "parallel_subject_groups", column: "ClassGroupId");
        migrationBuilder.CreateIndex(name: "IX_parallel_subject_groups_TenantId_AcademicSessionId_ClassGroupId_IsActive", table: "parallel_subject_groups", columns: new[] { "TenantId", "AcademicSessionId", "ClassGroupId", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_parallel_subject_group_members_ClassSubjectId", table: "parallel_subject_group_members", column: "ClassSubjectId");
        migrationBuilder.CreateIndex(name: "IX_parallel_subject_group_members_ParallelSubjectGroupId_ClassSubjectId", table: "parallel_subject_group_members", columns: new[] { "ParallelSubjectGroupId", "ClassSubjectId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_parallel_subject_group_members_TenantId_ClassSubjectId", table: "parallel_subject_group_members", columns: new[] { "TenantId", "ClassSubjectId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "parallel_subject_group_members");
        migrationBuilder.DropTable(name: "parallel_subject_groups");
    }
}
