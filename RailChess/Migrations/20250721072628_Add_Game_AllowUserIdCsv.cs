using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailChess.Migrations
{
    /// <inheritdoc />
    public partial class Add_Game_AllowUserIdCsv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowUserIdCsv",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameName",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThinkSecsPerGame",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThinkSecsPerTurn",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowUserIdCsv",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GameName",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ThinkSecsPerGame",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ThinkSecsPerTurn",
                table: "Games");
        }
    }
}
