using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("AMISTADES")]
    public class Amistad
    {
        [Column("usuario_id1")]
        public int IdUsuario1 { get; set; }

        [Column("usuario_id2")]
        public int IdUsuario2 { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("fecha_solicitud")]
        public DateTime FechaSolicitud { get; set; }
    }
}
