using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OtobusBiletRezervasyon;

#nullable disable

namespace OtobusBiletRezervasyon.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260415202000_AddStationNameCityUniqueIndex")]
    public partial class AddStationNameCityUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_stations_name_city",
                table: "stations",
                columns: new[] { "name", "city" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stations_name_city",
                table: "stations");
        }
    }
}
