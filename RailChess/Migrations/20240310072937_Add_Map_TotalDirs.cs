using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailChess.Migrations
{
    /// <inheritdoc />
    public partial class Add_Map_TotalDirs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalDirections",
                table: "Maps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalDirections",
                table: "Maps");
        }
    }
}
