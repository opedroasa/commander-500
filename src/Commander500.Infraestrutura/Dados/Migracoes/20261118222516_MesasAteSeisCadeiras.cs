using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Commander500.Infraestrutura.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class MesasAteSeisCadeiras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Cadeiras_Numero",
                table: "Cadeiras");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cadeiras_Numero",
                table: "Cadeiras",
                sql: "\"Numero\" BETWEEN 1 AND 6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Cadeiras_Numero",
                table: "Cadeiras");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cadeiras_Numero",
                table: "Cadeiras",
                sql: "\"Numero\" BETWEEN 1 AND 4");
        }
    }
}
