using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OtobusBiletRezervasyon;

#nullable disable

namespace OtobusBiletRezervasyon.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260415213000_AddPasswordResetTokenUniqueIndex")]
    public partial class AddPasswordResetTokenUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_password_resets_token",
                table: "password_resets",
                column: "token",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_password_resets_token",
                table: "password_resets");
        }
    }
}
