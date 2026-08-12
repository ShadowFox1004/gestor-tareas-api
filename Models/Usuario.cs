using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_tareas_api.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual ICollection<Proyecto> ProyectosPropios { get; set; } = new List<Proyecto>();
        public virtual ICollection<MiembroProyecto> Membresias { get; set; } = new List<MiembroProyecto>();
        public virtual ICollection<Tarea> TareasAsignadas { get; set; } = new List<Tarea>();
        public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
    }
}
