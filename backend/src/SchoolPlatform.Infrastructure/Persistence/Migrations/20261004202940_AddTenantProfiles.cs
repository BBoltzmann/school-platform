using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Motto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Mission = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Vision = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ShortAbout = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    LogoDataUrl = table.Column<string>(type: "text", nullable: true),
                    IconDataUrl = table.Column<string>(type: "text", nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_profiles_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_profiles_TenantId",
                table: "tenant_profiles",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_profiles");
        }
    }
}
