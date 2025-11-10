using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Venda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNumeroVendaSequenceAndOptimizedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NumeroVendaSequences",
                columns: table => new
                {
                    FilialId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UltimoNumero = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Versao = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumeroVendaSequences", x => x.FilialId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId",
                table: "Vendas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId_Data",
                table: "Vendas",
                columns: new[] { "FilialId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId_NumeroVenda_Unique",
                table: "Vendas",
                columns: new[] { "FilialId", "NumeroVenda" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId_Status",
                table: "Vendas",
                columns: new[] { "FilialId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NumeroVendaSequences");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId_Data",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId_NumeroVenda_Unique",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId_Status",
                table: "Vendas");
        }
    }
}
