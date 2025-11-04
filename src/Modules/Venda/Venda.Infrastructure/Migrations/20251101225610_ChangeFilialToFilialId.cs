using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Venda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFilialToFilialId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Filial",
                table: "Vendas");

            migrationBuilder.AddColumn<Guid>(
                name: "FilialId",
                table: "Vendas",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilialId",
                table: "Vendas");

            migrationBuilder.AddColumn<string>(
                name: "Filial",
                table: "Vendas",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
