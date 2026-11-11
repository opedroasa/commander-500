using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Commander500.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StagesAndScryfall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StageId",
                table: "Rounds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ArtUrl",
                table: "Commanders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorIdentity",
                table: "Commanders",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Commanders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScryfallCheckedAt",
                table: "Commanders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScryfallId",
                table: "Commanders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Stages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stages_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_StageId",
                table: "Rounds",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_Commanders_ScryfallId",
                table: "Commanders",
                column: "ScryfallId",
                unique: true,
                filter: "\"ScryfallId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Stages_SeasonId_Date",
                table: "Stages",
                columns: new[] { "SeasonId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stages_SeasonId_Number",
                table: "Stages",
                columns: new[] { "SeasonId", "Number" });

            // Existing rounds: one stage (etapa) per season + date, numbered by date; then link each round to it.
            migrationBuilder.Sql("""
                INSERT INTO "Stages" ("SeasonId", "Date", "Number")
                SELECT "SeasonId", "Date", ROW_NUMBER() OVER (PARTITION BY "SeasonId" ORDER BY "Date")
                FROM (SELECT DISTINCT "SeasonId", "Date" FROM "Rounds") AS d;

                UPDATE "Rounds" AS r SET "StageId" = s."Id"
                FROM "Stages" AS s
                WHERE s."SeasonId" = r."SeasonId" AND s."Date" = r."Date";
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_Stages_StageId",
                table: "Rounds",
                column: "StageId",
                principalTable: "Stages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_Stages_StageId",
                table: "Rounds");

            migrationBuilder.DropTable(
                name: "Stages");

            migrationBuilder.DropIndex(
                name: "IX_Rounds_StageId",
                table: "Rounds");

            migrationBuilder.DropIndex(
                name: "IX_Commanders_ScryfallId",
                table: "Commanders");

            migrationBuilder.DropColumn(
                name: "StageId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "ArtUrl",
                table: "Commanders");

            migrationBuilder.DropColumn(
                name: "ColorIdentity",
                table: "Commanders");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Commanders");

            migrationBuilder.DropColumn(
                name: "ScryfallCheckedAt",
                table: "Commanders");

            migrationBuilder.DropColumn(
                name: "ScryfallId",
                table: "Commanders");
        }
    }
}
