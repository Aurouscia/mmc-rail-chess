using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailChess.Migrations
{
    /// <inheritdoc />
    public partial class Create_MapTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<int>(type: "INTEGER", nullable: false),
                    TopoData = table.Column<string>(type: "TEXT", nullable: true),
                    ImgFileName = table.Column<string>(type: "TEXT", nullable: true),
                    StationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ExcStationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LineCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Maps");
        }
    }
}
