using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorTareas.GestionPermisos.Migrations
{
    /// <inheritdoc />
    public partial class CrearSolicitudesPermiso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesPermiso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdSolicitante = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermisoSolicitado = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    IdAprobador = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesPermiso", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesPermiso");
        }
    }
}
