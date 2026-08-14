using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using gestor_tareas_api.Data;
using gestor_tareas_api.Models;
using gestor_tareas_api.DTOs;
namespace gestor_tareas_api.Controllers
{
    // Nota: La ruta base aquí es más corta para poder manejar las rutas híbridas en cada método
    [Route("api/v1")]
    [ApiController]
    [Authorize]
    public class TareasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TareasController(AppDbContext context)
        {
            _context = context;
        }

        private int ObtenerUsuarioId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // GET: /api/v1/proyectos/{projectId}/tareas
        [HttpGet("proyectos/{projectId}/tareas")]
        public async Task<IActionResult> ObtenerTareasPaginadas(
            int projectId,
            [FromQuery] EstadoTarea? estado,
            [FromQuery] PrioridadTarea? prioridad,
            [FromQuery] int? asignadoAId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = ObtenerUsuarioId();

            // 1. Validar permisos: ¿Es dueño o miembro del proyecto? (Cualquiera puede leer)
            var proyecto = await _context.Proyectos
                .Include(p => p.Miembros)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (proyecto == null)
                return NotFound(new { mensaje = "Proyecto no encontrado." });

            bool tieneAcceso = proyecto.PropietarioId == userId ||
                               proyecto.Miembros.Any(m => m.UsuarioId == userId);

            if (!tieneAcceso)
                return StatusCode(403, new { mensaje = "No tienes acceso a las tareas de este proyecto." });

            // 2. Construir la consulta base
            var query = _context.Tareas
                .Include(t => t.AsignadoA)
                .Include(t => t.Adjuntos)
                .Include(t => t.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .Where(t => t.ProyectoId == projectId)
                .AsQueryable();

            // 3. Aplicar filtros si existen en la URL
            if (estado.HasValue)
                query = query.Where(t => t.Estado == estado.Value);

            if (prioridad.HasValue)
                query = query.Where(t => t.Prioridad == prioridad.Value);

            if (asignadoAId.HasValue)
                query = query.Where(t => t.AsignadoAId == asignadoAId.Value);

            // 4. Calcular el total de elementos (crucial para armar la botonera de paginación en React)
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // 5. Aplicar paginación (Skip y Take) y Proyección
            var tareas = await query
                .OrderBy(t => t.Id) // EF Core exige ordenar antes de usar Skip
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TareaResponseDTO
                {
                    Id = t.Id,
                    Título = t.Título,
                    Descripción = t.Descripción,
                    Estado = t.Estado.ToString(),
                    Prioridad = t.Prioridad.ToString(),
                    FechaVencimiento = t.FechaVencimiento,
                    AsignadoA = t.AsignadoA != null ? t.AsignadoA.Nombre : "Sin asignar",
                    AsignadoAId = t.AsignadoAId,
                    Adjuntos = t.Adjuntos.Select(a => new AdjuntoResponseDTO
                    {
                        Id = a.Id,
                        NombreArchivo = a.NombreArchivo,
                        RutaArchivo = "/Uploads/" + a.RutaRelativa
                    }).ToList(),
                    Comentarios = t.Comentarios.Select(c => new ComentarioResponseDTO
                    {
                        Id = c.Id,
                        Contenido = c.Contenido,
                        FechaCreacion = c.FechaCreacion,
                        UsuarioId = c.UsuarioId,
                        NombreUsuario = c.Usuario != null ? c.Usuario.Nombre : "Usuario desconocido",
                        EmailUsuario = c.Usuario != null ? c.Usuario.Email : "Sin correo"
                    }).ToList()
                })
                .ToListAsync();

            // 6. Retornar el resultado estructurado
            return Ok(new
            {
                Data = tareas,
                Paginacion = new
                {
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                }
            });
        }

        // POST: /api/v1/proyectos/{projectId}/tareas
        [HttpPost("proyectos/{projectId}/tareas")]
        public async Task<IActionResult> CrearTarea(int projectId, [FromBody] TareaCreateDTO request)
        {
            var userId = ObtenerUsuarioId();

            // 1. Validar permisos: Debe ser Owner o Editor del proyecto
            var miembro = await _context.MiembrosProyectos
                .FirstOrDefaultAsync(m => m.ProyectoId == projectId && m.UsuarioId == userId);

            var proyecto = await _context.Proyectos.FindAsync(projectId);

            if (proyecto == null) return NotFound(new { mensaje = "Proyecto no encontrado." });

            bool esOwner = proyecto.PropietarioId == userId;
            bool esEditor = miembro != null && (miembro.Rol == RolProyecto.Owner || miembro.Rol == RolProyecto.Editor);

            if (!esOwner && !esEditor)
                return StatusCode(403, new { mensaje = "Solo los propietarios y editores pueden crear tareas." });

            // 2. Crear la tarea
            var nuevaTarea = new Tarea
            {
                Título = request.Título,
                Descripción = request.Descripción,
                Prioridad = request.Prioridad,
                AsignadoAId = request.AsignadoAId,
                FechaVencimiento = request.FechaVencimiento,
                ProyectoId = projectId,
                Estado = EstadoTarea.ToDo // Toda tarea nueva empieza en ToDo
            };

            _context.Tareas.Add(nuevaTarea);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Tarea creada exitosamente", id = nuevaTarea.Id });
        }

        // PATCH: /api/v1/tareas/{id}/estado 
        // Ideal para cuando mueves una tarjeta en el tablero Kanban
        [HttpPatch("tareas/{id}/estado")]
        public async Task<IActionResult> CambiarEstadoTarea(int id, [FromBody] TareaEstadoUpdateDTO request)
        {
            var userId = ObtenerUsuarioId();

            // Buscamos la tarea e incluimos el proyecto para validar permisos
            var tarea = await _context.Tareas
                .Include(t => t.Proyecto)
                .ThenInclude(p => p.Miembros)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound(new { mensaje = "Tarea no encontrada." });

            // Validar permisos
            bool esOwner = tarea.Proyecto.PropietarioId == userId;
            bool esEditor = tarea.Proyecto.Miembros.Any(m => m.UsuarioId == userId && (m.Rol == RolProyecto.Owner || m.Rol == RolProyecto.Editor));

            if (!esOwner && !esEditor)
                return StatusCode(403, new { mensaje = "No tienes permiso para mover esta tarea." });

            // Actualizar solo el estado
            tarea.Estado = request.Estado;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estado de tarea actualizado correctamente." });
        }

        // PUT: /api/v1/tareas/{id}
        [HttpPut("tareas/{id}")]
        public async Task<IActionResult> EditarTarea(int id, [FromBody] TareaCreateDTO request)
        {
            var userId = ObtenerUsuarioId();

            var tarea = await _context.Tareas
                .Include(t => t.Proyecto)
                .ThenInclude(p => p.Miembros)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound(new { mensaje = "Tarea no encontrada." });

            bool esOwner = tarea.Proyecto.PropietarioId == userId;
            bool esEditor = tarea.Proyecto.Miembros.Any(m => m.UsuarioId == userId && (m.Rol == RolProyecto.Owner || m.Rol == RolProyecto.Editor));

            if (!esOwner && !esEditor)
                return StatusCode(403, new { mensaje = "No tienes permiso para editar esta tarea." });

            // Actualizar datos
            tarea.Título = request.Título;
            tarea.Descripción = request.Descripción;
            tarea.Prioridad = request.Prioridad;
            tarea.AsignadoAId = request.AsignadoAId;
            tarea.FechaVencimiento = request.FechaVencimiento;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Tarea actualizada correctamente." });
        }

        // DELETE: /api/v1/tareas/{id}
        [HttpDelete("tareas/{id}")]
        public async Task<IActionResult> EliminarTarea(int id)
        {
            var userId = ObtenerUsuarioId();

            var tarea = await _context.Tareas
                .Include(t => t.Proyecto)
                .ThenInclude(p => p.Miembros)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound(new { mensaje = "Tarea no encontrada." });

            bool esOwner = tarea.Proyecto.PropietarioId == userId;
            bool esEditor = tarea.Proyecto.Miembros.Any(m => m.UsuarioId == userId && (m.Rol == RolProyecto.Owner || m.Rol == RolProyecto.Editor));

            if (!esOwner && !esEditor)
                return StatusCode(403, new { mensaje = "No tienes permiso para eliminar esta tarea." });

            _context.Tareas.Remove(tarea);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Tarea eliminada correctamente." });
        }
    }
}
