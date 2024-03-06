using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailChess.Migrations
{
    /// <inheritdoc />
    public partial class Add_User_Elo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Elo",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Elo",
                table: "Users");
        }
    }
}
