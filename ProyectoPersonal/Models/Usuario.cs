using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("V_USUARIOS_ROLES")]
    public class Usuario
    {
        [Key]
        [Column("usuario_id")]
        public int IdUsuario { get; set; }
        [Required(ErrorMessage = "¡Necesitamos un nombre!")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 20 caracteres.")]
        [Column("nombre_usuario")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio para activar la cuenta.")]
        [EmailAddress(ErrorMessage = "Introduce un correo válido (ejemplo@correo.com).")]
        [Column("email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres por seguridad.")]
        [Column("password")]
        public string Password { get; set; }
        [Column("corazones_actuales")]

        public int CorazonesActuales { get; set; }
        [Column("corazones_maximos")]
        public int CorazonesMaximos { get; set; }
        [Column("avatar")]
        public string Avatar { get; set;  }
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
        [Column("ultima_recarga")]
        public DateTime? UltimaRecarga { get; set; }
        [Column("rol_id")]
        public int RolId { get; set; }
        [Column("anuncios_vistos_hoy")]
        public int AnunciosVistosHoy { get; set; }

        [Column("fecha_ultimo_anuncio")]
        public DateTime? FechaUltimoAnuncio { get; set; }
        [Column("esta_banido")]
        public bool? EstaBanido { get; set; }

        [Column("motivo_banimento")]
        public string? MotivoBanimento { get; set; }

        [Column("fecha_fin_banimento")]
        public DateTime? FechaFinBanimento { get; set; }
        [Column("activo")]
        public bool Activo { get; set; }
        [Column("tokenmail")]
        public string? TokenMail { get; set;  }
        [Column("nombre_rol")]
        public string? NombreRol { get; set; }
    }
}
