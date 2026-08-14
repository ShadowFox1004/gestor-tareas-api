using Microsoft.EntityFrameworkCore;
using gestor_tareas_api.Models;

namespace gestor_tareas_api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Proyecto> Proyectos { get; set; }
        public DbSet<MiembroProyecto> MiembrosProyectos { get; set; }
        public DbSet<Tarea> Tareas { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<Adjunto> Adjuntos { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configurar Llave Primaria Compuesta para la tabla intermedia
            modelBuilder.Entity<MiembroProyecto>()
                .HasKey(mp => new { mp.ProyectoId, mp.UsuarioId });

            // 2. Prevenir Multiple Cascade Paths en SQL Server

            // Un usuario crea proyectos (Si se borra el usuario, NO borrar el proyecto automáticamente)
            modelBuilder.Entity<Proyecto>()
                .HasOne(p => p.Propietario)
                .WithMany(u => u.ProyectosPropios)
                .HasForeignKey(p => p.PropietarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Un usuario tiene tareas asignadas (Si se borra el usuario, la tarea solo pierde el asignado)
            modelBuilder.Entity<Tarea>()
                .HasOne(t => t.AsignadoA)
                .WithMany(u => u.TareasAsignadas)
                .HasForeignKey(t => t.AsignadoAId)
                .OnDelete(DeleteBehavior.SetNull);

            // Un usuario hace comentarios (Si se borra el usuario, conservar la tarea pero restringir borrado)
            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Comentarios)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Asegurar que los emails sean únicos a nivel de base de datos
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 3. Configurar RefreshToken
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.Usuario)
                .WithMany()
                .HasForeignKey(rt => rt.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Datos semilla
            // Usuarios
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario { Id = 1, Nombre = "Rayber", Email = "rayber@admin.com", PasswordHash = "hash_temporal_123", FechaRegistro = DateTime.UtcNow },
                new Usuario { Id = 2, Nombre = "Merali", Email = "merali@user.com", PasswordHash = "hash_temporal_456", FechaRegistro = DateTime.UtcNow }
            );

            // Proyectos
            modelBuilder.Entity<Proyecto>().HasData(
                new Proyecto { Id = 1, Nombre = "Sistema Trello Clon", Descripción = "Proyecto Capstone final", Color = "#FF5733", PropietarioId = 1, FechaCreacion = DateTime.UtcNow },
                new Proyecto { Id = 2, Nombre = "Auditoría de Software", Descripción = "Proyecto de revisión", Color = "#33FF57", PropietarioId = 2, FechaCreacion = DateTime.UtcNow }
            );

            // Miembros del Proyecto (Asignar los dueños)
            modelBuilder.Entity<MiembroProyecto>().HasData(
                new MiembroProyecto { ProyectoId = 1, UsuarioId = 1, Rol = RolProyecto.Owner },
                new MiembroProyecto { ProyectoId = 2, UsuarioId = 2, Rol = RolProyecto.Owner },
                new MiembroProyecto { ProyectoId = 1, UsuarioId = 2, Rol = RolProyecto.Editor } // Merali colabora en el proyecto 1
            );

            // Tareas (5 tareas repartidas)
            modelBuilder.Entity<Tarea>().HasData(
                new Tarea { Id = 1, Título = "Diseñar Base de Datos", Descripción = "Crear modelos en EF Core", Estado = EstadoTarea.Done, Prioridad = PrioridadTarea.Alta, ProyectoId = 1, AsignadoAId = 1 },
                new Tarea { Id = 2, Título = "Implementar JWT", Descripción = "Configurar autenticación", Estado = EstadoTarea.InProgress, Prioridad = PrioridadTarea.Alta, ProyectoId = 1, AsignadoAId = 1 },
                new Tarea { Id = 3, Título = "Crear UI en React", Descripción = "Componentes Kanban", Estado = EstadoTarea.ToDo, Prioridad = PrioridadTarea.Media, ProyectoId = 1, AsignadoAId = 2 },
                new Tarea { Id = 4, Título = "Revisión de requerimientos", Descripción = "Leer el documento base", Estado = EstadoTarea.Done, Prioridad = PrioridadTarea.Baja, ProyectoId = 2, AsignadoAId = 2 },
                new Tarea { Id = 5, Título = "Redactar informe", Descripción = "Documentar hallazgos", Estado = EstadoTarea.ToDo, Prioridad = PrioridadTarea.Media, ProyectoId = 2, AsignadoAId = 2 }
            );
        }
    }
}