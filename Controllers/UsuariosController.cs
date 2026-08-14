using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gestor_tareas_api.Data;
using gestor_tareas_api.Models;

namespace gestor_tareas_api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize] // Protegemos la ruta para que solo usuarios logueados la usen
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarUsuarios([FromQuery] string email)
        {
            // Si nos mandan algo vacío o muy corto, devolvemos una lista vacía
            if (string.IsNullOrWhiteSpace(email) || email.Length < 2)
                return Ok(new List<object>());

            // Buscamos coincidencias (ignorando mayúsculas/minúsculas automáticamente por EF Core)
            var usuarios = await _context.Usuarios
                .Where(u => u.Email.Contains(email))
                .Select(u => new { u.Id, u.Email, u.Nombre }) // SOLO devolvemos los datos necesarios
                .Take(5) // Limitamos a 5 resultados para no sobrecargar la UI
                .ToListAsync();

            return Ok(usuarios);
        }
    }
}
