using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimetablePlanningCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "class_subject_requirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodsPerWeek = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_subject_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_subject_requirements_academic_sessions_AcademicSessio~",
                        column: x => x.AcademicSessionId,
                        principalTable: "academic_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_subject_requirements_class_groups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "class_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_subject_requirements_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "timetable_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timetable_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timetable_settings_academic_sessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "academic_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "timetable_days",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimetableSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timetable_days", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timetable_days_timetable_settings_TimetableSettingsId",
                        column: x => x.TimetableSettingsId,
                        principalTable: "timetable_settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "timetable_non_teaching_blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimetableDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timetable_non_teaching_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timetable_non_teaching_blocks_timetable_days_TimetableDayId",
                        column: x => x.TimetableDayId,
                        principalTable: "timetable_days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_subject_requirements_AcademicSessionId",
                table: "class_subject_requirements",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_class_subject_requirements_ClassGroupId",
                table: "class_subject_requirements",
                column: "ClassGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_class_subject_requirements_SubjectId",
                table: "class_subject_requirements",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_class_subject_requirements_TenantId_AcademicSessionId_Clas~1",
                table: "class_subject_requirements",
                columns: new[] { "TenantId", "AcademicSessionId", "ClassGroupId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_subject_requirements_TenantId_AcademicSessionId_Class~",
                table: "class_subject_requirements",
                columns: new[] { "TenantId", "AcademicSessionId", "ClassGroupId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_timetable_days_TenantId_TimetableSettingsId_DayOfWeek",
                table: "timetable_days",
                columns: new[] { "TenantId", "TimetableSettingsId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_timetable_days_TimetableSettingsId",
                table: "timetable_days",
                column: "TimetableSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_timetable_non_teaching_blocks_TenantId_TimetableDayId_IsAct~",
                table: "timetable_non_teaching_blocks",
                columns: new[] { "TenantId", "TimetableDayId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_timetable_non_teaching_blocks_TimetableDayId",
                table: "timetable_non_teaching_blocks",
                column: "TimetableDayId");

            migrationBuilder.CreateIndex(
                name: "IX_timetable_settings_AcademicSessionId",
                table: "timetable_settings",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_timetable_settings_TenantId_AcademicSessionId",
                table: "timetable_settings",
                columns: new[] { "TenantId", "AcademicSessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "class_subject_requirements");

            migrationBuilder.DropTable(
                name: "timetable_non_teaching_blocks");

            migrationBuilder.DropTable(
                name: "timetable_days");

            migrationBuilder.DropTable(
                name: "timetable_settings");
        }
    }
}
