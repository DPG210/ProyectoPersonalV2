using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("INVITACIONES")]
    public class Invitacion
    {
        [Key]
        [Column("invitacion_id")]
        public int IdInvitacion { get; set; }

        [Column("partida_id")]
        public int IdPartida { get; set; }

        [Column("usuario_emisor_id")]
        public int IdEmisor { get; set; }

        [Column("usuario_receptor_id")]
        public int IdReceptor { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("fecha_invitacion")]
        public DateTime FechaInvitacion { get; set; }
    }
}