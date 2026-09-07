using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAdmissionApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuardianHomeAddress",
                table: "admission_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianName",
                table: "admission_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianOccupation",
                table: "admission_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianOfficeAddress",
                table: "admission_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianPhone",
                table: "admission_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentClass",
                table: "admission_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousSchoolName",
                table: "admission_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Religion",
                table: "admission_applications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuardianHomeAddress",
                table: "admission_applications");

            migrationBuilder.DropColumn(
                name: "GuardianName",
                table: "admission_applications");

            migrationBuilder.DropColumn(
                name: "GuardianOccupation",
                table: "admission_applications");

            migrationBuilder.DropColumn(
                name: "GuardianOfficeAddress",
                table: "admission_applications");

            migrationBuilder.DropColumn(
                name: "GuardianPhone",
                table: "admission_applications");

            migrationBuilder.DropColumn(
                name: "PresentClass",
                table: "admission_applications");

            migrationBuilder.DropColumn(
                name: "PreviousSchoolName",
                table: "admission_applications");

            migrationBuilder.DropColumn(
                name: "Religion",
                table: "admission_applications");
        }
    }
}
