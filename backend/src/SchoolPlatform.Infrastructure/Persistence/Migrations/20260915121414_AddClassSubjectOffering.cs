using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassSubjectOffering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsesCustomSubjectOffering",
                table: "class_groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "class_subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_subjects_class_groups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "class_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_class_subjects_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_ClassGroupId_SubjectId",
                table: "class_subjects",
                columns: new[] { "ClassGroupId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_SubjectId",
                table: "class_subjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_TenantId_ClassGroupId",
                table: "class_subjects",
                columns: new[] { "TenantId", "ClassGroupId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "class_subjects");

            migrationBuilder.DropColumn(
                name: "UsesCustomSubjectOffering",
                table: "class_groups");
        }
    }
}
