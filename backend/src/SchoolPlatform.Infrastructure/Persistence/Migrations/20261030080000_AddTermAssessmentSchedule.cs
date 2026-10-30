using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolPlatform.Infrastructure.Persistence;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SchoolPlatformDbContext))]
[Migration("20261030080000_AddTermAssessmentSchedule")]
public sealed class AddTermAssessmentSchedule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "FirstHalfWeight", "SecondHalfWeight", "FinalExamWeight" })
        {
            migrationBuilder.AddColumn<decimal>(
                name: column, table: "academic_terms", type: "numeric(5,2)",
                nullable: false, defaultValue: column == "FinalExamWeight" ? 60m : 20m);
        }
        foreach (var column in new[] {
            "FirstHalfDueDate", "MidtermBreakStartDate", "ResumeDate", "MidtermReportDueDate",
            "SecondHalfDueDate", "FinalExamDueDate", "FinalReportDueDate" })
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: column, table: "academic_terms", type: "date", nullable: true);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] {
            "FirstHalfWeight", "SecondHalfWeight", "FinalExamWeight",
            "FirstHalfDueDate", "MidtermBreakStartDate", "ResumeDate", "MidtermReportDueDate",
            "SecondHalfDueDate", "FinalExamDueDate", "FinalReportDueDate" })
        {
            migrationBuilder.DropColumn(name: column, table: "academic_terms");
        }
    }
}
