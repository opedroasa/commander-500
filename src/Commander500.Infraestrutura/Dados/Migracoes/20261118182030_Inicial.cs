using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Commander500.Infraestrutura.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Administradores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HashSenha = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administradores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comandantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdScryfall = table.Column<Guid>(type: "uuid", nullable: true),
                    UrlArte = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UrlImagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdentidadeCor = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    DataConsultaScryfall = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comandantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jogadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Apelido = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jogadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Temporadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    ConfiguracaoTravada = table.Column<bool>(type: "boolean", nullable: false),
                    PontosVitoria = table.Column<int>(type: "integer", nullable: false),
                    PontosEmpate = table.Column<int>(type: "integer", nullable: false),
                    PontosBye1 = table.Column<int>(type: "integer", nullable: false),
                    PontosBye2 = table.Column<int>(type: "integer", nullable: false),
                    PontosBye3 = table.Column<int>(type: "integer", nullable: false),
                    MinutosPadraoRodada = table.Column<int>(type: "integer", nullable: false),
                    TurnosExtrasAposTempo = table.Column<int>(type: "integer", nullable: false),
                    MinimoPartidasPercentualComandante = table.Column<int>(type: "integer", nullable: false),
                    CriteriosDesempate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Temporadas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Decks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdJogador = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UrlLista = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ObservacaoRevisao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decks_Jogadores_IdJogador",
                        column: x => x.IdJogador,
                        principalTable: "Jogadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Etapas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdTemporada = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etapas_Temporadas_IdTemporada",
                        column: x => x.IdTemporada,
                        principalTable: "Temporadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DecksComandantes",
                columns: table => new
                {
                    IdDeck = table.Column<int>(type: "integer", nullable: false),
                    IdComandante = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecksComandantes", x => new { x.IdDeck, x.IdComandante });
                    table.ForeignKey(
                        name: "FK_DecksComandantes_Comandantes_IdComandante",
                        column: x => x.IdComandante,
                        principalTable: "Comandantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DecksComandantes_Decks_IdDeck",
                        column: x => x.IdDeck,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rodadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdTemporada = table.Column<int>(type: "integer", nullable: false),
                    IdEtapa = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IniciadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DuracaoMinutos = table.Column<int>(type: "integer", nullable: false),
                    EncerradaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rodadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rodadas_Etapas_IdEtapa",
                        column: x => x.IdEtapa,
                        principalTable: "Etapas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rodadas_Temporadas_IdTemporada",
                        column: x => x.IdTemporada,
                        principalTable: "Temporadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdRodada = table.Column<int>(type: "integer", nullable: false),
                    IdJogador = table.Column<int>(type: "integer", nullable: false),
                    IdDeck = table.Column<int>(type: "integer", nullable: false),
                    Bye = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participacoes_Decks_IdDeck",
                        column: x => x.IdDeck,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Participacoes_Jogadores_IdJogador",
                        column: x => x.IdJogador,
                        principalTable: "Jogadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Participacoes_Rodadas_IdRodada",
                        column: x => x.IdRodada,
                        principalTable: "Rodadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mesas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdRodada = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Resultado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdParticipacaoVencedora = table.Column<int>(type: "integer", nullable: true),
                    AlteradaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AlteradaPor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesas_Participacoes_IdParticipacaoVencedora",
                        column: x => x.IdParticipacaoVencedora,
                        principalTable: "Participacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Mesas_Rodadas_IdRodada",
                        column: x => x.IdRodada,
                        principalTable: "Rodadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cadeiras",
                columns: table => new
                {
                    IdMesa = table.Column<int>(type: "integer", nullable: false),
                    IdParticipacao = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cadeiras", x => new { x.IdMesa, x.IdParticipacao });
                    table.CheckConstraint("CK_Cadeiras_Numero", "\"Numero\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_Cadeiras_Mesas_IdMesa",
                        column: x => x.IdMesa,
                        principalTable: "Mesas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cadeiras_Participacoes_IdParticipacao",
                        column: x => x.IdParticipacao,
                        principalTable: "Participacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Administradores_Email",
                table: "Administradores",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cadeiras_IdMesa_Numero",
                table: "Cadeiras",
                columns: new[] { "IdMesa", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cadeiras_IdParticipacao",
                table: "Cadeiras",
                column: "IdParticipacao");

            migrationBuilder.CreateIndex(
                name: "IX_Comandantes_IdScryfall",
                table: "Comandantes",
                column: "IdScryfall",
                unique: true,
                filter: "\"IdScryfall\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Comandantes_NomeNormalizado",
                table: "Comandantes",
                column: "NomeNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decks_IdJogador",
                table: "Decks",
                column: "IdJogador");

            migrationBuilder.CreateIndex(
                name: "IX_DecksComandantes_IdComandante",
                table: "DecksComandantes",
                column: "IdComandante");

            migrationBuilder.CreateIndex(
                name: "IX_Etapas_IdTemporada_Data",
                table: "Etapas",
                columns: new[] { "IdTemporada", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Etapas_IdTemporada_Numero",
                table: "Etapas",
                columns: new[] { "IdTemporada", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_IdParticipacaoVencedora",
                table: "Mesas",
                column: "IdParticipacaoVencedora");

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_IdRodada_Numero",
                table: "Mesas",
                columns: new[] { "IdRodada", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_IdDeck",
                table: "Participacoes",
                column: "IdDeck");

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_IdJogador",
                table: "Participacoes",
                column: "IdJogador");

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_IdRodada_IdJogador",
                table: "Participacoes",
                columns: new[] { "IdRodada", "IdJogador" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rodadas_IdEtapa",
                table: "Rodadas",
                column: "IdEtapa");

            migrationBuilder.CreateIndex(
                name: "IX_Rodadas_IdTemporada_Numero",
                table: "Rodadas",
                columns: new[] { "IdTemporada", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administradores");

            migrationBuilder.DropTable(
                name: "Cadeiras");

            migrationBuilder.DropTable(
                name: "DecksComandantes");

            migrationBuilder.DropTable(
                name: "Mesas");

            migrationBuilder.DropTable(
                name: "Comandantes");

            migrationBuilder.DropTable(
                name: "Participacoes");

            migrationBuilder.DropTable(
                name: "Decks");

            migrationBuilder.DropTable(
                name: "Rodadas");

            migrationBuilder.DropTable(
                name: "Jogadores");

            migrationBuilder.DropTable(
                name: "Etapas");

            migrationBuilder.DropTable(
                name: "Temporadas");
        }
    }
}
