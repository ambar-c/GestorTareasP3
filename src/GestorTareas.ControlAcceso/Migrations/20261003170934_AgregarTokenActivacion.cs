using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorTareas.ControlAcceso.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTokenActivacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "HashTokenActivacion",
                table: "Usuarios",
                type: "varbinary(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VencimientoActivacion",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_HashTokenActivacion",
                table: "Usuarios",
                column: "HashTokenActivacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_HashTokenActivacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "HashTokenActivacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "VencimientoActivacion",
                table: "Usuarios");
        }
    }
}
