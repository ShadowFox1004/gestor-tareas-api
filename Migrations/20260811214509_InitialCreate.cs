using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace gestor_tareas_api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proyectos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripción = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PropietarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proyectos_Usuarios_PropietarioId",
                        column: x => x.PropietarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MiembrosProyectos",
                columns: table => new
                {
                    ProyectoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiembrosProyectos", x => new { x.ProyectoId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_MiembrosProyectos_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MiembrosProyectos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tareas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Título = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripción = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProyectoId = table.Column<int>(type: "int", nullable: false),
                    AsignadoAId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tareas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tareas_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tareas_Usuarios_AsignadoAId",
                        column: x => x.AsignadoAId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Adjuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RutaRelativa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TamañoBytes = table.Column<long>(type: "bigint", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TareaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adjuntos_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comentarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TareaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comentarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comentarios_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comentarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Email", "FechaRegistro", "Nombre", "PasswordHash" },
                values: new object[,]
                {
                    { 1, "rayber@admin.com", new DateTime(2026, 8, 11, 21, 45, 9, 301, DateTimeKind.Utc).AddTicks(7835), "Rayber", "hash_temporal_123" },
                    { 2, "merali@user.com", new DateTime(2026, 8, 11, 21, 45, 9, 301, DateTimeKind.Utc).AddTicks(7836), "Merali", "hash_temporal_456" }
                });

            migrationBuilder.InsertData(
                table: "Proyectos",
                columns: new[] { "Id", "Color", "Descripción", "FechaCreacion", "Nombre", "PropietarioId" },
                values: new object[,]
                {
                    { 1, "#FF5733", "Proyecto Capstone final", new DateTime(2026, 8, 11, 21, 45, 9, 301, DateTimeKind.Utc).AddTicks(7913), "Sistema Trello Clon", 1 },
                    { 2, "#33FF57", "Proyecto de revisión", new DateTime(2026, 8, 11, 21, 45, 9, 301, DateTimeKind.Utc).AddTicks(7914), "Auditoría de Software", 2 }
                });

            migrationBuilder.InsertData(
                table: "MiembrosProyectos",
                columns: new[] { "ProyectoId", "UsuarioId", "Rol" },
                values: new object[,]
                {
                    { 1, 1, 0 },
                    { 1, 2, 1 },
                    { 2, 2, 0 }
                });

            migrationBuilder.InsertData(
                table: "Tareas",
                columns: new[] { "Id", "AsignadoAId", "Descripción", "Estado", "FechaVencimiento", "Prioridad", "ProyectoId", "Título" },
                values: new object[,]
                {
                    { 1, 1, "Crear modelos en EF Core", 2, null, 2, 1, "Diseñar Base de Datos" },
                    { 2, 1, "Configurar autenticación", 1, null, 2, 1, "Implementar JWT" },
                    { 3, 2, "Componentes Kanban", 0, null, 1, 1, "Crear UI en React" },
                    { 4, 2, "Leer el documento base", 2, null, 0, 2, "Revisión de requerimientos" },
                    { 5, 2, "Documentar hallazgos", 0, null, 1, 2, "Redactar informe" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adjuntos_TareaId",
                table: "Adjuntos",
                column: "TareaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_TareaId",
                table: "Comentarios",
                column: "TareaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_UsuarioId",
                table: "Comentarios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosProyectos_UsuarioId",
                table: "MiembrosProyectos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_PropietarioId",
                table: "Proyectos",
                column: "PropietarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_AsignadoAId",
                table: "Tareas",
                column: "AsignadoAId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_ProyectoId",
                table: "Tareas",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adjuntos");

            migrationBuilder.DropTable(
                name: "Comentarios");

            migrationBuilder.DropTable(
                name: "MiembrosProyectos");

            migrationBuilder.DropTable(
                name: "Tareas");

            migrationBuilder.DropTable(
                name: "Proyectos");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
