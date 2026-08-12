using gestor_tareas_api.Models;
using System.ComponentModel.DataAnnotations;

namespace gestor_tareas_api.DTOs
{
    public class ProyectoCreateDTO
    {
        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(500)]
        public string Descripción { get; set; }

        [MaxLength(7)]
        public string Color { get; set; }
    }

    public class ProyectoResponseDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripción { get; set; }
        public string Color { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string RolUsuarioActual { get; set; } // Nos dirá si el usuario logueado es Owner, Editor o Viewer
    }

    public class InvitarMiembroDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public RolProyecto Rol { get; set; } // Debería ser Editor o Viewer
    }
}
