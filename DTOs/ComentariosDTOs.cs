using System.ComponentModel.DataAnnotations;

namespace gestor_tareas_api.DTOs
{
    public class ComentarioCreateDTO
    {
        [Required]
        public string Contenido { get; set; }
    }
}
