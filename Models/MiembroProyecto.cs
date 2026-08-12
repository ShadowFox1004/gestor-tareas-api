using System.ComponentModel.DataAnnotations;

namespace gestor_tareas_api.Models
{
    public class MiembroProyecto
    {
        public int ProyectoId { get; set; }
        public virtual Proyecto Proyecto { get; set; }

        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }

        [Required]
        public RolProyecto Rol { get; set; }
    }
}
