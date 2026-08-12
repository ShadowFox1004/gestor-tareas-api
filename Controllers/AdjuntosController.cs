using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using gestor_tareas_api.Data;
using gestor_tareas_api.Models;

namespace gestor_tareas_api.Controllers
{
    [Route("api/v1")]
    [ApiController]
    [Authorize]
    public class AdjuntosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _uploadFolder;

        public AdjuntosController(AppDbContext context)
        {
            _context = context;
            // Definimos la carpeta "Uploads" en la raíz del proyecto para guardar los archivos
            _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        private int ObtenerUsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // POST: /api/v1/tareas/{id}/adjuntos
        [HttpPost("tareas/{id}/adjuntos")]
        public async Task<IActionResult> SubirAdjunto(int id, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { mensaje = "No se proporcionó ningún archivo." });

            // 1. Regla de negocio: Tamaño máximo 5MB (5 * 1024 * 1024 bytes)
            if (archivo.Length > 5242880)
                return BadRequest(new { mensaje = "El archivo excede el límite permitido de 5 MB." });

            // 2. Regla de negocio: Validar tipo MIME
            var permitidos = new[] { "image/jpeg", "image/png", "application/pdf" };
            if (!permitidos.Contains(archivo.ContentType))
                return BadRequest(new { mensaje = "Tipo de archivo no permitido. Solo se aceptan imágenes (JPG, PNG) y PDFs." });

            var userId = ObtenerUsuarioId();
            var tarea = await _context.Tareas.Include(t => t.Proyecto).ThenInclude(p => p.Miembros)
                                             .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound(new { mensaje = "Tarea no encontrada." });

            bool tieneAcceso = tarea.Proyecto.PropietarioId == userId || tarea.Proyecto.Miembros.Any(m => m.UsuarioId == userId);
            if (!tieneAcceso) return StatusCode(403, new { mensaje = "No tienes acceso para subir archivos a esta tarea." });

            // 3. Generar un nombre único para evitar sobreescritura (Ej: 3f8a9b-archivo.pdf)
            var nombreUnico = Guid.NewGuid().ToString() + "_" + archivo.FileName;
            var rutaCompleta = Path.Combine(_uploadFolder, nombreUnico);

            // Guardar físicamente en el disco
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Guardar el registro en base de datos
            var adjunto = new Adjunto
            {
                NombreArchivo = archivo.FileName,
                RutaRelativa = nombreUnico, // Solo guardamos el nombre generado
                TamañoBytes = archivo.Length,
                TareaId = id,
                FechaSubida = DateTime.UtcNow
            };

            _context.Adjuntos.Add(adjunto);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Archivo subido exitosamente.", id = adjunto.Id });
        }

        // GET: /api/v1/adjuntos/{id}
        [HttpGet("adjuntos/{id}")]
        public async Task<IActionResult> DescargarAdjunto(int id)
        {
            var userId = ObtenerUsuarioId();
            var adjunto = await _context.Adjuntos.Include(a => a.Tarea).ThenInclude(t => t.Proyecto).ThenInclude(p => p.Miembros)
                                                 .FirstOrDefaultAsync(a => a.Id == id);

            if (adjunto == null) return NotFound(new { mensaje = "Archivo no encontrado." });

            bool tieneAcceso = adjunto.Tarea.Proyecto.PropietarioId == userId || adjunto.Tarea.Proyecto.Miembros.Any(m => m.UsuarioId == userId);
            if (!tieneAcceso) return StatusCode(403, new { mensaje = "No tienes permiso para descargar este archivo." });

            var rutaCompleta = Path.Combine(_uploadFolder, adjunto.RutaRelativa);
            if (!System.IO.File.Exists(rutaCompleta))
                return NotFound(new { mensaje = "El archivo físico ya no existe en el servidor." });

            var mimeType = adjunto.NombreArchivo.EndsWith(".pdf") ? "application/pdf" : "image/jpeg";
            return PhysicalFile(rutaCompleta, mimeType, adjunto.NombreArchivo);
        }

        // DELETE: /api/v1/adjuntos/{id}
        [HttpDelete("adjuntos/{id}")]
        public async Task<IActionResult> EliminarAdjunto(int id)
        {
            var userId = ObtenerUsuarioId();
            var adjunto = await _context.Adjuntos.Include(a => a.Tarea).ThenInclude(t => t.Proyecto).ThenInclude(p => p.Miembros)
                                                 .FirstOrDefaultAsync(a => a.Id == id);

            if (adjunto == null) return NotFound(new { mensaje = "Archivo no encontrado." });

            bool esOwner = adjunto.Tarea.Proyecto.PropietarioId == userId;
            bool esEditor = adjunto.Tarea.Proyecto.Miembros.Any(m => m.UsuarioId == userId && (m.Rol == RolProyecto.Owner || m.Rol == RolProyecto.Editor));

            if (!esOwner && !esEditor) return StatusCode(403, new { mensaje = "No tienes permiso para eliminar este archivo." });

            // Borrar de la base de datos
            _context.Adjuntos.Remove(adjunto);
            await _context.SaveChangesAsync();

            // Borrar del disco duro
            var rutaCompleta = Path.Combine(_uploadFolder, adjunto.RutaRelativa);
            if (System.IO.File.Exists(rutaCompleta))
            {
                System.IO.File.Delete(rutaCompleta);
            }

            return Ok(new { mensaje = "Archivo eliminado correctamente." });
        }
    }
}
