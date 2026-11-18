using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Commander500.Infraestrutura.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class PontuacaoFinalRegulamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Etapas_IdTemporada_Data",
                table: "Etapas");

            migrationBuilder.AddColumn<string>(
                name: "ModoPontuacao",
                table: "Temporadas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ColocacaoNaEtapa"); // temporadas que já existiam passam a usar o modo padrão

            migrationBuilder.AddColumn<int>(
                name: "PontosDemaisColocados",
                table: "Temporadas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PontosDerrota",
                table: "Temporadas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PontosPorColocacao",
                table: "Temporadas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "5,4,3,2");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PrazoListas",
                table: "Temporadas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProximaEtapaEm",
                table: "Temporadas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Final",
                table: "Etapas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RodadasPrevistas",
                table: "Etapas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Regulamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Conteudo = table.Column<string>(type: "text", nullable: false),
                    AlteradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AlteradoPor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regulamentos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Etapas_IdTemporada_Data_Final",
                table: "Etapas",
                columns: new[] { "IdTemporada", "Data", "Final" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Regulamentos");

            migrationBuilder.DropIndex(
                name: "IX_Etapas_IdTemporada_Data_Final",
                table: "Etapas");

            migrationBuilder.DropColumn(
                name: "ModoPontuacao",
                table: "Temporadas");

            migrationBuilder.DropColumn(
                name: "PontosDemaisColocados",
                table: "Temporadas");

            migrationBuilder.DropColumn(
                name: "PontosDerrota",
                table: "Temporadas");

            migrationBuilder.DropColumn(
                name: "PontosPorColocacao",
                table: "Temporadas");

            migrationBuilder.DropColumn(
                name: "PrazoListas",
                table: "Temporadas");

            migrationBuilder.DropColumn(
                name: "ProximaEtapaEm",
                table: "Temporadas");

            migrationBuilder.DropColumn(
                name: "Final",
                table: "Etapas");

            migrationBuilder.DropColumn(
                name: "RodadasPrevistas",
                table: "Etapas");

            migrationBuilder.CreateIndex(
                name: "IX_Etapas_IdTemporada_Data",
                table: "Etapas",
                columns: new[] { "IdTemporada", "Data" },
                unique: true);
        }
    }
}
