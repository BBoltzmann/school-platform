using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeesManagementCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fee_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fee_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fee_structures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AudienceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AudienceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_structures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "student_fee_charges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeStructureId = table.Column<Guid>(type: "uuid", nullable: true),
                    FeeStructureLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_fee_charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_fee_charges_fee_items_FeeItemId",
                        column: x => x.FeeItemId,
                        principalTable: "fee_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_fee_charges_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_structure_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeStructureId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_structure_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fee_structure_lines_fee_items_FeeItemId",
                        column: x => x.FeeItemId,
                        principalTable: "fee_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fee_structure_lines_fee_structures_FeeStructureId",
                        column: x => x.FeeStructureId,
                        principalTable: "fee_structures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fee_payment_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeePaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentFeeChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_payment_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fee_payment_allocations_fee_payments_FeePaymentId",
                        column: x => x.FeePaymentId,
                        principalTable: "fee_payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fee_payment_allocations_student_fee_charges_StudentFeeCharg~",
                        column: x => x.StudentFeeChargeId,
                        principalTable: "student_fee_charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fee_items_TenantId_Name",
                table: "fee_items",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_payment_allocations_FeePaymentId",
                table: "fee_payment_allocations",
                column: "FeePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_fee_payment_allocations_StudentFeeChargeId",
                table: "fee_payment_allocations",
                column: "StudentFeeChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_fee_payment_allocations_TenantId_FeePaymentId_StudentFeeCha~",
                table: "fee_payment_allocations",
                columns: new[] { "TenantId", "FeePaymentId", "StudentFeeChargeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_payments_TenantId_ReceiptNumber",
                table: "fee_payments",
                columns: new[] { "TenantId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_lines_FeeItemId",
                table: "fee_structure_lines",
                column: "FeeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_lines_FeeStructureId",
                table: "fee_structure_lines",
                column: "FeeStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_lines_TenantId_FeeStructureId_FeeItemId",
                table: "fee_structure_lines",
                columns: new[] { "TenantId", "FeeStructureId", "FeeItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_structures_TenantId_AcademicTermId_Name",
                table: "fee_structures",
                columns: new[] { "TenantId", "AcademicTermId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_student_fee_charges_FeeItemId",
                table: "student_fee_charges",
                column: "FeeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_student_fee_charges_StudentId",
                table: "student_fee_charges",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_fee_charges_TenantId_StudentId_AcademicTermId",
                table: "student_fee_charges",
                columns: new[] { "TenantId", "StudentId", "AcademicTermId" });

            migrationBuilder.CreateIndex(
                name: "IX_student_fee_charges_TenantId_StudentId_AcademicTermId_FeeSt~",
                table: "student_fee_charges",
                columns: new[] { "TenantId", "StudentId", "AcademicTermId", "FeeStructureLineId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fee_payment_allocations");

            migrationBuilder.DropTable(
                name: "fee_structure_lines");

            migrationBuilder.DropTable(
                name: "fee_payments");

            migrationBuilder.DropTable(
                name: "student_fee_charges");

            migrationBuilder.DropTable(
                name: "fee_structures");

            migrationBuilder.DropTable(
                name: "fee_items");
        }
    }
}
