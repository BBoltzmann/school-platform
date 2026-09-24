using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeStructureStudentAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fee_structure_student_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeStructureId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_structure_student_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fee_structure_student_assignments_fee_structures_FeeStructu~",
                        column: x => x.FeeStructureId,
                        principalTable: "fee_structures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fee_structure_student_assignments_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_student_assignments_FeeStructureId",
                table: "fee_structure_student_assignments",
                column: "FeeStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_student_assignments_StudentId",
                table: "fee_structure_student_assignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_student_assignments_TenantId_FeeStructureId",
                table: "fee_structure_student_assignments",
                columns: new[] { "TenantId", "FeeStructureId" });

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_student_assignments_TenantId_FeeStructureId_S~",
                table: "fee_structure_student_assignments",
                columns: new[] { "TenantId", "FeeStructureId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_student_assignments_TenantId_StudentId",
                table: "fee_structure_student_assignments",
                columns: new[] { "TenantId", "StudentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fee_structure_student_assignments");
        }
    }
}
