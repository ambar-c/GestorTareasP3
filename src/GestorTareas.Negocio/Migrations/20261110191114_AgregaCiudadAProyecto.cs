using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorTareas.Negocio.Migrations
{
    /// <inheritdoc />
    public partial class AgregaCiudadAProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "Proyectos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "Proyectos");
        }
    }
}
