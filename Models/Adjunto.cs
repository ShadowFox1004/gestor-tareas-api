using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_tareas_api.Models
{
    public class Adjunto
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string NombreArchivo { get; set; }

        [Required]
        public string RutaRelativa { get; set; }

        public long TamañoBytes { get; set; }

        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        public int TareaId { get; set; }
        [ForeignKey(nameof(TareaId))]
        public virtual Tarea Tarea { get; set; }
    }
}
