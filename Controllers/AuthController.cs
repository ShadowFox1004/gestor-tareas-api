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
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Register([FromForm] RegisterRequestDTO request, IFormFile? imagenPerfil)
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

            // Subir imagen de perfil si se proporciona
            if (imagenPerfil != null && imagenPerfil.Length > 0)
            {
                if (imagenPerfil.Length > 5242880)
                    return BadRequest(new { mensaje = "El archivo de imagen excede el límite de 5 MB." });

                var permitidos = new[] { "image/jpeg", "image/png" };
                if (!permitidos.Contains(imagenPerfil.ContentType))
                    return BadRequest(new { mensaje = "Solo se aceptan imágenes JPG y PNG." });

                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var nombreUnico = Guid.NewGuid().ToString() + "_" + imagenPerfil.FileName;
                var rutaCompleta = Path.Combine(uploadFolder, nombreUnico);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await imagenPerfil.CopyToAsync(stream);
                }

                nuevoUsuario.ImagenPerfil = nombreUnico;
            }

            // 4. Guardar en base de datos
            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

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

            // 4. Generar el Refresh Token
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString(),
                UsuarioId = usuario.Id,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddDays(7), // Dura 7 días
                Revocado = false
            };

            _context.RefreshTokens.Add(refreshToken);
            _context.SaveChanges();

            return Ok(new AuthResponseDTO 
            { 
                Token = token, 
                RefreshToken = refreshToken.Token,
                Mensaje = "Login exitoso" 
            });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshRequestDTO request)
        {
            // 1. Validar el Refresh Token en base de datos
            var storedRefreshToken = _context.RefreshTokens
                .Include(rt => rt.Usuario)
                .FirstOrDefault(rt => rt.Token == request.RefreshToken);

            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            {
                return Unauthorized(new { mensaje = "Refresh Token inválido o expirado." });
            }

            // 2. Rotar el Refresh Token (revocar el viejo y generar uno nuevo)
            storedRefreshToken.Revocado = true;
            storedRefreshToken.FechaRevocacion = DateTime.UtcNow;

            var nuevoRefreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString(),
                UsuarioId = storedRefreshToken.UsuarioId,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddDays(7),
                Revocado = false
            };

            _context.RefreshTokens.Add(nuevoRefreshToken);
            _context.SaveChanges();

            // 3. Generar el nuevo Access Token
            var nuevoAccessToken = GenerarJwtToken(storedRefreshToken.Usuario);

            return Ok(new AuthResponseDTO
            {
                Token = nuevoAccessToken,
                RefreshToken = nuevoRefreshToken.Token,
                Mensaje = "Token refrescado correctamente"
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout([FromBody] LogoutRequestDTO request)
        {
            var storedRefreshToken = _context.RefreshTokens
                .FirstOrDefault(rt => rt.Token == request.RefreshToken);

            if (storedRefreshToken != null)
            {
                storedRefreshToken.Revocado = true;
                storedRefreshToken.FechaRevocacion = DateTime.UtcNow;
                _context.SaveChanges();
            }

            return Ok(new { mensaje = "Sesión cerrada correctamente." });
        }

        [HttpGet("perfil")]
        [Authorize] // <- ESTO ES LO QUE PROTEGE EL ENDPOINT
        public async Task<IActionResult> ObtenerPerfil()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios.FindAsync(userId);

            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });

            return Ok(new
            {
                mensaje = "¡Tienes acceso a esta ruta protegida!",
                usuario = usuario.Nombre,
                correo = usuario.Email,
                imagenPerfil = !string.IsNullOrEmpty(usuario.ImagenPerfil) ? "/Uploads/" + usuario.ImagenPerfil : null
            });
        }

        // POST: /api/v1/auth/perfil/imagen
        [HttpPost("perfil/imagen")]
        [Authorize]
        public async Task<IActionResult> ActualizarImagenPerfil(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { mensaje = "No se proporcionó ningún archivo." });

            if (archivo.Length > 5242880)
                return BadRequest(new { mensaje = "El archivo excede el límite de 5 MB." });

            var permitidos = new[] { "image/jpeg", "image/png" };
            if (!permitidos.Contains(archivo.ContentType))
                return BadRequest(new { mensaje = "Tipo de archivo no permitido. Solo se aceptan imágenes (JPG, PNG)." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios.FindAsync(userId);

            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var nombreUnico = Guid.NewGuid().ToString() + "_" + archivo.FileName;
            var rutaCompleta = Path.Combine(uploadFolder, nombreUnico);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Borrar imagen vieja si existe
            if (!string.IsNullOrEmpty(usuario.ImagenPerfil))
            {
                var rutaVieja = Path.Combine(uploadFolder, usuario.ImagenPerfil);
                if (System.IO.File.Exists(rutaVieja))
                {
                    System.IO.File.Delete(rutaVieja);
                }
            }

            usuario.ImagenPerfil = nombreUnico;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Imagen de perfil actualizada correctamente.", imagenUrl = "/Uploads/" + nombreUnico });
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
                Expires = DateTime.UtcNow.AddMinutes(15), // El Access Token dura 15 minutos
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
