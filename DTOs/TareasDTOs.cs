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
        public int? AsignadoAId { get; set; }
        public List<AdjuntoResponseDTO> Adjuntos { get; set; } = new List<AdjuntoResponseDTO>();
        public List<ComentarioResponseDTO> Comentarios { get; set; } = new List<ComentarioResponseDTO>();
    }

    public class AdjuntoResponseDTO
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; }
        public string RutaArchivo { get; set; }
    }

    public class ComentarioResponseDTO
    {
        public int Id { get; set; }
        public string Contenido { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string EmailUsuario { get; set; }
    }
}

