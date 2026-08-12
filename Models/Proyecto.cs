using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_tareas_api.Models
{
    public class Proyecto
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(500)]
        public string Descripción { get; set; }

        [MaxLength(7)] // Para guardar hexadecimanes como #FFFFFF
        public string Color { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Foreign Key explícita
        public int PropietarioId { get; set; }
        [ForeignKey(nameof(PropietarioId))]
        public virtual Usuario Propietario { get; set; }

        // Navegación
        public virtual ICollection<MiembroProyecto> Miembros { get; set; } = new List<MiembroProyecto>();
        public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
    }
}
