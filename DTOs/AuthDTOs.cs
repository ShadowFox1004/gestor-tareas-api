using System.ComponentModel.DataAnnotations;

namespace gestor_tareas_api.DTOs
{
    public class RegisterRequestDTO
    {
        [Required]
        public string Nombre { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, MinLength(6)]
        public string Password { get; set; }
    }

    public class LoginRequestDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }

    public class AuthResponseDTO
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Mensaje { get; set; }
    }

    public class RefreshRequestDTO
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string RefreshToken { get; set; }
    }

    public class LogoutRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
