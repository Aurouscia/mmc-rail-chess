using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailChess.Migrations
{
    /// <inheritdoc />
    public partial class Add_Competition_Scoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParticipantUserIdCsv",
                table: "Competitions",
                newName: "ParticipantsJson");

            migrationBuilder.AddColumn<string>(
                name: "HomepageUrl",
                table: "Competitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoringJson",
                table: "CompetitionMatches",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomepageUrl",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "ScoringJson",
                table: "CompetitionMatches");

            migrationBuilder.RenameColumn(
                name: "ParticipantsJson",
                table: "Competitions",
                newName: "ParticipantUserIdCsv");
        }
    }
}
