using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeachingAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teaching_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teaching_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_teaching_assignments_academic_sessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "academic_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teaching_assignments_class_groups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "class_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teaching_assignments_staff_members_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalTable: "staff_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teaching_assignments_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_AcademicSessionId",
                table: "teaching_assignments",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_ClassGroupId",
                table: "teaching_assignments",
                column: "ClassGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_StaffMemberId",
                table: "teaching_assignments",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_SubjectId",
                table: "teaching_assignments",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_TenantId_AcademicSessionId_ClassGroupI~",
                table: "teaching_assignments",
                columns: new[] { "TenantId", "AcademicSessionId", "ClassGroupId", "SubjectId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_TenantId_StaffMemberId_AcademicSession~",
                table: "teaching_assignments",
                columns: new[] { "TenantId", "StaffMemberId", "AcademicSessionId", "ClassGroupId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_TenantId_StaffMemberId_IsActive",
                table: "teaching_assignments",
                columns: new[] { "TenantId", "StaffMemberId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teaching_assignments");
        }
    }
}
