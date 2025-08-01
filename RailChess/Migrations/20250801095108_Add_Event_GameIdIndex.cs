using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailChess.Migrations
{
    /// <inheritdoc />
    public partial class Add_Event_GameIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Events_GameId",
                table: "Events",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_GameId",
                table: "Events");
        }
    }
}
