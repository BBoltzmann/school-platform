using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "UserId", table: "staff_members", type: "uuid", nullable: true);
            migrationBuilder.CreateIndex(name: "IX_staff_members_TenantId_UserId", table: "staff_members", columns: new[] { "TenantId", "UserId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_staff_members_UserId", table: "staff_members", column: "UserId");
            migrationBuilder.AddForeignKey(name: "FK_staff_members_users_UserId", table: "staff_members", column: "UserId", principalTable: "users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_staff_members_users_UserId", table: "staff_members");
            migrationBuilder.DropIndex(name: "IX_staff_members_TenantId_UserId", table: "staff_members");
            migrationBuilder.DropIndex(name: "IX_staff_members_UserId", table: "staff_members");
            migrationBuilder.DropColumn(name: "UserId", table: "staff_members");
        }
    }
}
