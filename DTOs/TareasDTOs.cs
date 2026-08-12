using gestor_tareas_api.Models;
using System.ComponentModel.DataAnnotations;

namespace gestor_tareas_api.DTOs
{
    public class TareaCreateDTO
    {
        [Required, MaxLength(200)]
        public string Título { get; set; }

        public string Descripción { get; set; }

        [Required]
        public PrioridadTarea Prioridad { get; set; }

        public int? AsignadoAId { get; set; }

        public DateTime? FechaVencimiento { get; set; }
    }

    public class TareaEstadoUpdateDTO
    {
        [Required]
        public EstadoTarea Estado { get; set; }
    }
    public class TareaResponseDTO
    {
        public int Id { get; set; }
        public string Título { get; set; }
        public string Descripción { get; set; }
        public string Estado { get; set; }
        public string Prioridad { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string AsignadoA { get; set; } // Devolveremos el nombre del usuario, no su entidad completa
    }
}
