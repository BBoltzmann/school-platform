using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassSubjectActiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "class_subjects",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_TenantId_ClassGroupId_IsActive",
                table: "class_subjects",
                columns: new[] { "TenantId", "ClassGroupId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_class_subjects_TenantId_ClassGroupId_IsActive",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "class_subjects");
        }
    }
}
