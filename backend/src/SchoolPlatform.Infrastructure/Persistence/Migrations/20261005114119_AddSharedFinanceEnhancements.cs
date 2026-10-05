using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedFinanceEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carry_forward_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carry_forward_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "discount_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CalculationType = table.Column<int>(type: "integer", nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discount_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "student_fee_adjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CarryForwardEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_fee_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_fee_adjustments_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "carry_forward_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarryForwardRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDebit = table.Column<bool>(type: "boolean", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceAdjustmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetAdjustmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carry_forward_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carry_forward_entries_carry_forward_runs_CarryForwardRunId",
                        column: x => x.CarryForwardRunId,
                        principalTable: "carry_forward_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_carry_forward_entries_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    AudienceType = table.Column<int>(type: "integer", nullable: false),
                    AcademicLevelId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmountOverride = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discount_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_discount_applications_discount_definitions_DiscountDefiniti~",
                        column: x => x.DiscountDefinitionId,
                        principalTable: "discount_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_discount_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_discount_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_discount_assignments_discount_applications_Discount~",
                        column: x => x.DiscountApplicationId,
                        principalTable: "discount_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_discount_assignments_discount_definitions_DiscountD~",
                        column: x => x.DiscountDefinitionId,
                        principalTable: "discount_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_discount_assignments_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carry_forward_entries_CarryForwardRunId",
                table: "carry_forward_entries",
                column: "CarryForwardRunId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_forward_entries_StudentId",
                table: "carry_forward_entries",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_carry_forward_entries_TenantId_CarryForwardRunId_StudentId",
                table: "carry_forward_entries",
                columns: new[] { "TenantId", "CarryForwardRunId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carry_forward_entries_TenantId_StudentId_SourceSessionId_So~",
                table: "carry_forward_entries",
                columns: new[] { "TenantId", "StudentId", "SourceSessionId", "SourceTermId", "TargetSessionId", "TargetTermId" });

            migrationBuilder.CreateIndex(
                name: "IX_carry_forward_runs_TenantId_IdempotencyKey",
                table: "carry_forward_runs",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_DiscountDefinitionId",
                table: "discount_applications",
                column: "DiscountDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_TenantId_DiscountDefinitionId_Academi~",
                table: "discount_applications",
                columns: new[] { "TenantId", "DiscountDefinitionId", "AcademicSessionId", "AcademicTermId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_TenantId_IdempotencyKey",
                table: "discount_applications",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discount_definitions_TenantId_Name",
                table: "discount_definitions",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_discount_assignments_DiscountApplicationId",
                table: "student_discount_assignments",
                column: "DiscountApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_student_discount_assignments_DiscountDefinitionId",
                table: "student_discount_assignments",
                column: "DiscountDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_student_discount_assignments_StudentId",
                table: "student_discount_assignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_discount_assignments_TenantId_DiscountApplicationId~",
                table: "student_discount_assignments",
                columns: new[] { "TenantId", "DiscountApplicationId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_discount_assignments_TenantId_StudentId_AcademicSes~",
                table: "student_discount_assignments",
                columns: new[] { "TenantId", "StudentId", "AcademicSessionId", "AcademicTermId" });

            migrationBuilder.CreateIndex(
                name: "IX_student_fee_adjustments_StudentId",
                table: "student_fee_adjustments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_fee_adjustments_TenantId_StudentId_AcademicSessionI~",
                table: "student_fee_adjustments",
                columns: new[] { "TenantId", "StudentId", "AcademicSessionId", "AcademicTermId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carry_forward_entries");

            migrationBuilder.DropTable(
                name: "student_discount_assignments");

            migrationBuilder.DropTable(
                name: "student_fee_adjustments");

            migrationBuilder.DropTable(
                name: "carry_forward_runs");

            migrationBuilder.DropTable(
                name: "discount_applications");

            migrationBuilder.DropTable(
                name: "discount_definitions");
        }
    }
}
