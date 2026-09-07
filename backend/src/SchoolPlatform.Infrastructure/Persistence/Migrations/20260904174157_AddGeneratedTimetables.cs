using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedTimetables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "generated_timetables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_timetables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "generated_timetable_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedTimetableId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_timetable_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_generated_timetable_entries_generated_timetables_GeneratedT~",
                        column: x => x.GeneratedTimetableId,
                        principalTable: "generated_timetables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_timetable_entries_GeneratedTimetableId",
                table: "generated_timetable_entries",
                column: "GeneratedTimetableId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_timetable_entries_TenantId_GeneratedTimetableId_C~",
                table: "generated_timetable_entries",
                columns: new[] { "TenantId", "GeneratedTimetableId", "ClassGroupId", "DayOfWeek", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_generated_timetable_entries_TenantId_GeneratedTimetableId_S~",
                table: "generated_timetable_entries",
                columns: new[] { "TenantId", "GeneratedTimetableId", "StaffMemberId", "DayOfWeek", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_generated_timetables_TenantId_AcademicSessionId",
                table: "generated_timetables",
                columns: new[] { "TenantId", "AcademicSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_generated_timetables_TenantId_AcademicTermId",
                table: "generated_timetables",
                columns: new[] { "TenantId", "AcademicTermId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_timetable_entries");

            migrationBuilder.DropTable(
                name: "generated_timetables");
        }
    }
}
