using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestorTareas.Negocio.Migrations
{
    /// <inheritdoc />
    public partial class AgregaPrioridadesIniciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Prioridades",
                columns: new[] { "Id", "Nivel", "Nombre" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 1, "Alta" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 2, "Media" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 3, "Baja" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Prioridades",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Prioridades",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Prioridades",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}
