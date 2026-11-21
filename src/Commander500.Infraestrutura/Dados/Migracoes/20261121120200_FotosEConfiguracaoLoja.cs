using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Commander500.Infraestrutura.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class FotosEConfiguracaoLoja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaminhoFoto",
                table: "Temporadas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegendaFoto",
                table: "Temporadas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaminhoFoto",
                table: "Etapas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegendaFoto",
                table: "Etapas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracoesLoja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LinkGrupoWhatsapp = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AlteradaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AlteradaPor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesLoja", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "CaminhoFoto",
                table: "Temporadas");

            migrationBuilder.DropColumn(
                name: "LegendaFoto",
                table: "Temporadas");

            migrationBuilder.DropColumn(
                name: "CaminhoFoto",
                table: "Etapas");

            migrationBuilder.DropColumn(
                name: "LegendaFoto",
                table: "Etapas");
        }
    }
}
