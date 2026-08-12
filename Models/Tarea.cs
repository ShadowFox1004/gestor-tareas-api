using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_tareas_api.Models
{
    public class Tarea
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Título { get; set; }

        public string Descripción { get; set; }

        public EstadoTarea Estado { get; set; } = EstadoTarea.ToDo;
        public PrioridadTarea Prioridad { get; set; } = PrioridadTarea.Media;

        public DateTime? FechaVencimiento { get; set; }

        // FK a Proyecto
        public int ProyectoId { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public virtual Proyecto Proyecto { get; set; }

        // FK a Usuario (Nullable)
        public int? AsignadoAId { get; set; }
        [ForeignKey(nameof(AsignadoAId))]
        public virtual Usuario AsignadoA { get; set; }

        // Navegación
        public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
        public virtual ICollection<Adjunto> Adjuntos { get; set; } = new List<Adjunto>();
    }
}
