using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Venda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizedIndexesWithDescendingOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendas_ClienteId_Data",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_Data",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId_Data",
                table: "Vendas");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_ClienteId_Data",
                table: "Vendas",
                columns: new[] { "ClienteId", "Data" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Covering_List",
                table: "Vendas",
                columns: new[] { "Data", "Status", "ClienteId", "FilialId" },
                descending: new[] { true, false, false, false });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Data",
                table: "Vendas",
                column: "Data",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId_Data",
                table: "Vendas",
                columns: new[] { "FilialId", "Data" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Status_Data",
                table: "Vendas",
                columns: new[] { "Status", "Data" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendas_ClienteId_Data",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_Covering_List",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_Data",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId_Data",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_Status_Data",
                table: "Vendas");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_ClienteId_Data",
                table: "Vendas",
                columns: new[] { "ClienteId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Data",
                table: "Vendas",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId_Data",
                table: "Vendas",
                columns: new[] { "FilialId", "Data" });
        }
    }
}
