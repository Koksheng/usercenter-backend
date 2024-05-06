using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace usercenter_backend.Migrations
{
    /// <inheritdoc />
    public partial class addPlanetCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanetCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanetCode",
                table: "AspNetUsers");
        }
    }
}
