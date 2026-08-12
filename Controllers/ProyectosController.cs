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
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize] // ¡Protege todas las rutas de este archivo!
    public class ProyectosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProyectosController(AppDbContext context)
        {
            _context = context;
        }

        // Método de ayuda para extraer el ID del usuario del token JWT
        private int ObtenerUsuarioId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim);
        }

        // GET: /api/v1/proyectos
        [HttpGet]
        public async Task<IActionResult> GetProyectos()
        {
            var userId = ObtenerUsuarioId();

            // Traer proyectos donde soy el dueño o estoy en la tabla de miembros
            var proyectos = await _context.Proyectos
                .Include(p => p.Miembros)
                .Where(p => p.PropietarioId == userId || p.Miembros.Any(m => m.UsuarioId == userId))
                .Select(p => new ProyectoResponseDTO
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripción = p.Descripción,
                    Color = p.Color,
                    FechaCreacion = p.FechaCreacion,
                    // Lógica rápida para determinar el rol del usuario en la respuesta
                    RolUsuarioActual = p.PropietarioId == userId ? "Owner" : p.Miembros.First(m => m.UsuarioId == userId).Rol.ToString()
                })
                .ToListAsync();

            return Ok(proyectos);
        }

        // POST: /api/v1/proyectos
        [HttpPost]
        public async Task<IActionResult> CrearProyecto([FromBody] ProyectoCreateDTO request)
        {
            var userId = ObtenerUsuarioId();

            // 1. Crear el proyecto en sí
            var nuevoProyecto = new Proyecto
            {
                Nombre = request.Nombre,
                Descripción = request.Descripción,
                Color = request.Color,
                PropietarioId = userId,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Proyectos.Add(nuevoProyecto);
            await _context.SaveChangesAsync(); // Guardamos para que SQL Server nos genere un Id

            // 2. Registrar al creador en la tabla intermedia de miembros como 'Owner'
            var miembro = new MiembroProyecto
            {
                ProyectoId = nuevoProyecto.Id,
                UsuarioId = userId,
                Rol = RolProyecto.Owner
            };

            _context.MiembrosProyectos.Add(miembro);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Proyecto creado exitosamente", id = nuevoProyecto.Id });
        }

        // PUT: /api/v1/proyectos/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarProyecto(int id, [FromBody] ProyectoCreateDTO request)
        {
            var userId = ObtenerUsuarioId();

            // 1. Buscar el proyecto y sus miembros
            var proyecto = await _context.Proyectos
                .Include(p => p.Miembros)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null)
                return NotFound(new { mensaje = "Proyecto no encontrado." });

            // 2. Regla de Autorización: ¿Es dueño o editor?
            bool esPropietario = proyecto.PropietarioId == userId;
            bool esEditor = proyecto.Miembros.Any(m => m.UsuarioId == userId && (m.Rol == RolProyecto.Owner || m.Rol == RolProyecto.Editor));

            if (!esPropietario && !esEditor)
                return StatusCode(403, new { mensaje = "No tienes permisos para editar este proyecto. Se requiere rol de Owner o Editor." });

            // 3. Aplicar los cambios
            proyecto.Nombre = request.Nombre;
            proyecto.Descripción = request.Descripción;
            proyecto.Color = request.Color;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Proyecto actualizado correctamente." });
        }

        // DELETE: /api/v1/proyectos/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProyecto(int id)
        {
            var userId = ObtenerUsuarioId();

            // 1. Buscar el proyecto, incluyendo sus tareas para validar
            var proyecto = await _context.Proyectos
                .Include(p => p.Tareas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null)
                return NotFound(new { mensaje = "Proyecto no encontrado." });

            // 2. Regla de Autorización: Solo el Propietario (Owner) puede borrarlo
            if (proyecto.PropietarioId != userId)
                return StatusCode(403, new { mensaje = "Solo el propietario del proyecto puede eliminarlo." });

            // 3. Regla de Negocio: No borrar si tiene tareas asociadas
            if (proyecto.Tareas.Any())
                return BadRequest(new { mensaje = "No se puede eliminar el proyecto porque tiene tareas asociadas. Debes eliminarlas o transferirlas primero." });

            // 4. Limpiar la tabla intermedia (Miembros) antes de borrar el proyecto
            var miembrosDelProyecto = _context.MiembrosProyectos.Where(m => m.ProyectoId == id);
            _context.MiembrosProyectos.RemoveRange(miembrosDelProyecto);

            // 5. Eliminar el proyecto
            _context.Proyectos.Remove(proyecto);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Proyecto eliminado correctamente." });
        }

        // POST: /api/v1/proyectos/{id}/miembros
        [HttpPost("{id}/miembros")]
        public async Task<IActionResult> InvitarMiembro(int id, [FromBody] InvitarMiembroDTO request)
        {
            var userId = ObtenerUsuarioId();
            var proyecto = await _context.Proyectos.FindAsync(id);

            if (proyecto == null) return NotFound(new { mensaje = "Proyecto no encontrado." });

            // Solo el Owner puede invitar
            if (proyecto.PropietarioId != userId)
                return StatusCode(403, new { mensaje = "Solo el propietario del proyecto puede invitar miembros." });

            // Buscar al usuario por correo
            var usuarioInvitado = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (usuarioInvitado == null)
                return NotFound(new { mensaje = "No existe un usuario registrado con ese correo electrónico." });

            // Verificar si ya es miembro
            var yaEsMiembro = await _context.MiembrosProyectos
                .AnyAsync(m => m.ProyectoId == id && m.UsuarioId == usuarioInvitado.Id);

            if (yaEsMiembro)
                return BadRequest(new { mensaje = "El usuario ya es miembro de este proyecto." });

            // Agregar a la tabla intermedia
            var nuevoMiembro = new MiembroProyecto
            {
                ProyectoId = id,
                UsuarioId = usuarioInvitado.Id,
                Rol = request.Rol
            };

            _context.MiembrosProyectos.Add(nuevoMiembro);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Usuario {usuarioInvitado.Nombre} añadido como {request.Rol}." });
        }

        // DELETE: /api/v1/proyectos/{id}/miembros/{miembroId}
        [HttpDelete("{id}/miembros/{miembroId}")]
        public async Task<IActionResult> RemoverMiembro(int id, int miembroId)
        {
            var userId = ObtenerUsuarioId();
            var proyecto = await _context.Proyectos.FindAsync(id);

            if (proyecto == null) return NotFound(new { mensaje = "Proyecto no encontrado." });

            if (proyecto.PropietarioId != userId)
                return StatusCode(403, new { mensaje = "Solo el propietario puede remover miembros." });

            if (proyecto.PropietarioId == miembroId)
                return BadRequest(new { mensaje = "El propietario no puede ser removido del proyecto." });

            var membresia = await _context.MiembrosProyectos
                .FirstOrDefaultAsync(m => m.ProyectoId == id && m.UsuarioId == miembroId);

            if (membresia == null)
                return NotFound(new { mensaje = "El usuario no es miembro de este proyecto." });

            _context.MiembrosProyectos.Remove(membresia);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Miembro removido exitosamente." });
        }
    }
}
