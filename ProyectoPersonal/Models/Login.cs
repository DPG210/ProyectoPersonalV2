using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("USER_SECURITY")]
    public class Login
    {
        [Key]
        [Column("id_seguridad")]
        public int IdSeguridad { get; set; }
        [Column("id_usuario")]
        public int IdUsuario { get; set; }
        [Column("salt")]
        public string Salt { get; set; }
        [Column("password_hash")]
        public string PasswordHash { get; set; }
    }
}
