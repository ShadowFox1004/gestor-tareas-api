using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_tareas_api.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Token { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime FechaExpiracion { get; set; }

        public bool Revocado { get; set; }

        public DateTime? FechaRevocacion { get; set; }

        public bool IsExpired => DateTime.UtcNow >= FechaExpiracion;
        public bool IsActive => !Revocado && !IsExpired;
    }
}
