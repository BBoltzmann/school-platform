using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations;

public partial class AddTimetableVersions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("VersionNumber", "generated_timetables", type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<bool>("IsActive", "generated_timetables", type: "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<DateTime>("ActivatedAtUtc", "generated_timetables", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>("SupersededAtUtc", "generated_timetables", type: "timestamp with time zone", nullable: true);
        migrationBuilder.Sql("UPDATE generated_timetables SET \"ActivatedAtUtc\" = \"GeneratedAtUtc\" WHERE \"ActivatedAtUtc\" IS NULL");
        migrationBuilder.DropIndex("IX_generated_timetables_TenantId_AcademicTermId", "generated_timetables");
        migrationBuilder.CreateIndex("IX_generated_timetables_active_scope", "generated_timetables", new[] { "TenantId", "AcademicSessionId", "AcademicTermId" }, unique: true, filter: "\"IsActive\" = true");
        migrationBuilder.CreateIndex("IX_generated_timetables_version_scope", "generated_timetables", new[] { "TenantId", "AcademicSessionId", "AcademicTermId", "VersionNumber" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_generated_timetables_active_scope", "generated_timetables");
        migrationBuilder.DropIndex("IX_generated_timetables_version_scope", "generated_timetables");
        migrationBuilder.CreateIndex("IX_generated_timetables_TenantId_AcademicTermId", "generated_timetables", new[] { "TenantId", "AcademicTermId" }, unique: true);
        migrationBuilder.DropColumn("VersionNumber", "generated_timetables");
        migrationBuilder.DropColumn("IsActive", "generated_timetables");
        migrationBuilder.DropColumn("ActivatedAtUtc", "generated_timetables");
        migrationBuilder.DropColumn("SupersededAtUtc", "generated_timetables");
    }
}
