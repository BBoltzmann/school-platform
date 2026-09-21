using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations;

public partial class AddParallelTimetableOccurrence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("ParallelSubjectGroupId", "generated_timetable_entries", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>("ParallelOccurrenceId", "generated_timetable_entries", type: "uuid", nullable: true);
        migrationBuilder.DropIndex("IX_generated_timetable_entries_TenantId_GeneratedTimetableId_ClassGroupId_DayOfWeek_PeriodNumber", "generated_timetable_entries");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_parallel_ordinary_slot", "generated_timetable_entries", new[] { "TenantId", "GeneratedTimetableId", "ClassGroupId", "DayOfWeek", "PeriodNumber" }, unique: true, filter: "\"ParallelOccurrenceId\" IS NULL");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_parallel_occurrence_slot", "generated_timetable_entries", new[] { "TenantId", "GeneratedTimetableId", "ClassGroupId", "DayOfWeek", "PeriodNumber", "ParallelOccurrenceId" }, unique: true, filter: "\"ParallelOccurrenceId\" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_ParallelSubjectGroupId", "generated_timetable_entries", "ParallelSubjectGroupId");
        migrationBuilder.AddForeignKey("FK_generated_timetable_entries_parallel_subject_groups_ParallelSubjectGroupId", "generated_timetable_entries", "ParallelSubjectGroupId", "parallel_subject_groups", "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_generated_timetable_entries_parallel_subject_groups_ParallelSubjectGroupId", "generated_timetable_entries");
        migrationBuilder.DropIndex("IX_generated_timetable_entries_ParallelSubjectGroupId", "generated_timetable_entries");
        migrationBuilder.DropIndex("IX_generated_timetable_entries_parallel_ordinary_slot", "generated_timetable_entries");
        migrationBuilder.DropIndex("IX_generated_timetable_entries_parallel_occurrence_slot", "generated_timetable_entries");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_TenantId_GeneratedTimetableId_ClassGroupId_DayOfWeek_PeriodNumber", "generated_timetable_entries", new[] { "TenantId", "GeneratedTimetableId", "ClassGroupId", "DayOfWeek", "PeriodNumber" }, unique: true);
        migrationBuilder.DropColumn("ParallelSubjectGroupId", "generated_timetable_entries");
        migrationBuilder.DropColumn("ParallelOccurrenceId", "generated_timetable_entries");
    }
}
