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
    [Route("api/v1")]
    [ApiController]
    [Authorize]
    public class ComentariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ComentariosController(AppDbContext context)
        {
            _context = context;
        }

        private int ObtenerUsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // POST: /api/v1/tareas/{id}/comentarios
        [HttpPost("tareas/{id}/comentarios")]
        public async Task<IActionResult> AgregarComentario(int id, [FromBody] ComentarioCreateDTO request)
        {
            var userId = ObtenerUsuarioId();

            var tarea = await _context.Tareas.Include(t => t.Proyecto).ThenInclude(p => p.Miembros)
                                             .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound(new { mensaje = "Tarea no encontrada." });

            // Validar si el usuario pertenece al proyecto (Owner o Miembro)
            bool tieneAcceso = tarea.Proyecto.PropietarioId == userId || tarea.Proyecto.Miembros.Any(m => m.UsuarioId == userId);
            if (!tieneAcceso) return StatusCode(403, new { mensaje = "No tienes acceso a esta tarea." });

            var comentario = new Comentario
            {
                Contenido = request.Contenido,
                TareaId = id,
                UsuarioId = userId,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Comentario agregado.", id = comentario.Id });
        }

        // DELETE: /api/v1/comentarios/{id}
        [HttpDelete("comentarios/{id}")]
        public async Task<IActionResult> EliminarComentario(int id)
        {
            var userId = ObtenerUsuarioId();
            var comentario = await _context.Comentarios.FindAsync(id);

            if (comentario == null) return NotFound(new { mensaje = "Comentario no encontrado." });

            // Regla estricta: Solo el autor puede eliminar su propio comentario
            if (comentario.UsuarioId != userId)
                return StatusCode(403, new { mensaje = "Solo puedes eliminar tus propios comentarios." });

            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Comentario eliminado." });
        }
    }
}
