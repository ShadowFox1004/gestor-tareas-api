using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using gestor_tareas_api.Data;
using gestor_tareas_api.Models;
using gestor_tareas_api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace gestor_tareas_api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequestDTO request)
        {
            // 1. Validar si el email ya existe
            if (_context.Usuarios.Any(u => u.Email == request.Email))
            {
                return BadRequest(new { mensaje = "El correo ya está registrado." });
            }

            // 2. Crear el nuevo usuario
            var nuevoUsuario = new Usuario
            {
                Nombre = request.Nombre,
                Email = request.Email,
                FechaRegistro = DateTime.UtcNow
            };

            // 3. Hashear la contraseña
            nuevoUsuario.PasswordHash = _passwordHasher.HashPassword(nuevoUsuario, request.Password);

            // 4. Guardar en base de datos
            _context.Usuarios.Add(nuevoUsuario);
            _context.SaveChanges();

            return Ok(new { mensaje = "Usuario registrado exitosamente." });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDTO request)
        {
            // 1. Buscar el usuario
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == request.Email);
            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Credenciales incorrectas." });
            }

            // 2. Verificar la contraseña (con protección contra hashes corruptos)
            PasswordVerificationResult result;
            try
            {
                result = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
            }
            catch (FormatException)
            {
                // Si el texto en la BD no es un hash válido (como nuestro dato semilla), lo tratamos como login fallido
                return Unauthorized(new { mensaje = "Credenciales incorrectas." });
            }

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { mensaje = "Credenciales incorrectas." });
            }

            // 3. Generar el JWT
            var token = GenerarJwtToken(usuario);

            return Ok(new AuthResponseDTO { Token = token, Mensaje = "Login exitoso" });
        }

        [HttpGet("perfil")]
        [Authorize] // <- ESTO ES LO QUE PROTEGE EL ENDPOINT
        public IActionResult ObtenerPerfil()
        {
            // Extraemos el email del usuario directamente desde el token que envió
            var email = User.FindFirstValue(ClaimTypes.Email);
            var nombre = User.FindFirstValue("nombre");

            return Ok(new
            {
                mensaje = "¡Tienes acceso a esta ruta protegida!",
                usuario = nombre,
                correo = email
            });
        }

        private string GenerarJwtToken(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

            // Claims: Información incrustada dentro del token (Ej: ID y Email)
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("nombre", usuario.Nombre)
        };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1), // El Access Token dura 1 hora
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
