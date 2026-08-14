using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestor_tareas_api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioImagenPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenPerfil",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Proyectos",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 8, 13, 23, 48, 10, 171, DateTimeKind.Utc).AddTicks(4608));

            migrationBuilder.UpdateData(
                table: "Proyectos",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaCreacion",
                value: new DateTime(2026, 8, 13, 23, 48, 10, 171, DateTimeKind.Utc).AddTicks(4609));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaRegistro", "ImagenPerfil" },
                values: new object[] { new DateTime(2026, 8, 13, 23, 48, 10, 171, DateTimeKind.Utc).AddTicks(4488), null });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaRegistro", "ImagenPerfil" },
                values: new object[] { new DateTime(2026, 8, 13, 23, 48, 10, 171, DateTimeKind.Utc).AddTicks(4490), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenPerfil",
                table: "Usuarios");

            migrationBuilder.UpdateData(
                table: "Proyectos",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaCreacion",
                value: new DateTime(2026, 8, 13, 23, 6, 42, 47, DateTimeKind.Utc).AddTicks(328));

            migrationBuilder.UpdateData(
                table: "Proyectos",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaCreacion",
                value: new DateTime(2026, 8, 13, 23, 6, 42, 47, DateTimeKind.Utc).AddTicks(329));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaRegistro",
                value: new DateTime(2026, 8, 13, 23, 6, 42, 47, DateTimeKind.Utc).AddTicks(200));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaRegistro",
                value: new DateTime(2026, 8, 13, 23, 6, 42, 47, DateTimeKind.Utc).AddTicks(239));
        }
    }
}
