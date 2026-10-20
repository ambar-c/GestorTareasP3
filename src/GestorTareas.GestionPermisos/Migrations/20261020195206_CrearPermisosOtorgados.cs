using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorTareas.GestionPermisos.Migrations
{
    /// <inheritdoc />
    public partial class CrearPermisosOtorgados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermisosOtorgados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permiso = table.Column<int>(type: "int", nullable: false),
                    FechaOtorgamiento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisosOtorgados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermisosOtorgados_IdUsuario_Permiso",
                table: "PermisosOtorgados",
                columns: new[] { "IdUsuario", "Permiso" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermisosOtorgados");
        }
    }
}
