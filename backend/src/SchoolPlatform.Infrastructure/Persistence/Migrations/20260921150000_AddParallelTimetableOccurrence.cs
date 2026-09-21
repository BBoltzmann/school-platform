using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations;

public partial class AddParallelTimetableOccurrence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_ClassGroupId", "generated_timetable_entries", "ClassGroupId");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_SubjectId", "generated_timetable_entries", "SubjectId");
        migrationBuilder.AddForeignKey("FK_generated_timetable_entries_class_groups_ClassGroupId", "generated_timetable_entries", "ClassGroupId", "class_groups", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("FK_generated_timetable_entries_subjects_SubjectId", "generated_timetable_entries", "SubjectId", "subjects", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddColumn<Guid>("ParallelSubjectGroupId", "generated_timetable_entries", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>("ParallelOccurrenceId", "generated_timetable_entries", type: "uuid", nullable: true);
        migrationBuilder.DropIndex("IX_generated_timetable_entries_TenantId_GeneratedTimetableId_C~", "generated_timetable_entries");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_parallel_ordinary_slot", "generated_timetable_entries", new[] { "TenantId", "GeneratedTimetableId", "ClassGroupId", "DayOfWeek", "PeriodNumber" }, unique: true, filter: "\"ParallelOccurrenceId\" IS NULL");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_parallel_occurrence_slot", "generated_timetable_entries", new[] { "TenantId", "GeneratedTimetableId", "ClassGroupId", "DayOfWeek", "PeriodNumber", "ParallelOccurrenceId", "SubjectId" }, unique: true, filter: "\"ParallelOccurrenceId\" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_ParallelSubjectGroupId", "generated_timetable_entries", "ParallelSubjectGroupId");
        migrationBuilder.AddCheckConstraint("CK_timetable_entry_parallel_pair", "generated_timetable_entries", "(\"ParallelSubjectGroupId\" IS NULL) = (\"ParallelOccurrenceId\" IS NULL)");
        migrationBuilder.AddForeignKey("FK_generated_timetable_entries_parallel_subject_groups_Paralle~", "generated_timetable_entries", "ParallelSubjectGroupId", "parallel_subject_groups", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_generated_timetable_entries_class_groups_ClassGroupId", "generated_timetable_entries");
        migrationBuilder.DropForeignKey("FK_generated_timetable_entries_subjects_SubjectId", "generated_timetable_entries");
        migrationBuilder.DropIndex("IX_generated_timetable_entries_ClassGroupId", "generated_timetable_entries");
        migrationBuilder.DropIndex("IX_generated_timetable_entries_SubjectId", "generated_timetable_entries");
        migrationBuilder.DropCheckConstraint("CK_timetable_entry_parallel_pair", "generated_timetable_entries");
        migrationBuilder.DropForeignKey("FK_generated_timetable_entries_parallel_subject_groups_Paralle~", "generated_timetable_entries");
        migrationBuilder.DropIndex("IX_generated_timetable_entries_ParallelSubjectGroupId", "generated_timetable_entries");
        migrationBuilder.DropIndex("IX_generated_timetable_entries_parallel_ordinary_slot", "generated_timetable_entries");
        migrationBuilder.DropIndex("IX_generated_timetable_entries_parallel_occurrence_slot", "generated_timetable_entries");
        migrationBuilder.CreateIndex("IX_generated_timetable_entries_TenantId_GeneratedTimetableId_C~", "generated_timetable_entries", new[] { "TenantId", "GeneratedTimetableId", "ClassGroupId", "DayOfWeek", "PeriodNumber" }, unique: true);
        migrationBuilder.DropColumn("ParallelSubjectGroupId", "generated_timetable_entries");
        migrationBuilder.DropColumn("ParallelOccurrenceId", "generated_timetable_entries");
    }
}
