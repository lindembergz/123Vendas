using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Venda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoIdIndexToItensVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ItensVenda_ProdutoId",
                table: "ItensVenda",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItensVenda_ProdutoId",
                table: "ItensVenda");
        }
    }
}
