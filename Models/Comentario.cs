using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_tareas_api.Models
{
    public class Comentario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Contenido { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public int TareaId { get; set; }
        [ForeignKey(nameof(TareaId))]
        public virtual Tarea Tarea { get; set; }

        public int UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public virtual Usuario Usuario { get; set; }
    }
}
