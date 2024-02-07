using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailChess.Migrations
{
    /// <inheritdoc />
    public partial class Create_GameTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HostUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    UseMapId = table.Column<int>(type: "INTEGER", nullable: false),
                    Started = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ended = table.Column<bool>(type: "INTEGER", nullable: false),
                    DurationMins = table.Column<int>(type: "INTEGER", nullable: false),
                    Steps = table.Column<int>(type: "INTEGER", nullable: false),
                    RandAlg = table.Column<int>(type: "INTEGER", nullable: false),
                    RandMin = table.Column<int>(type: "INTEGER", nullable: false),
                    RandMax = table.Column<int>(type: "INTEGER", nullable: false),
                    StucksToLose = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowReverseAtTerminal = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowTransfer = table.Column<bool>(type: "INTEGER", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
