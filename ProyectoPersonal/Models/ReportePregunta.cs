using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("REPORTES_PREGUNTAS")]
    public class ReportePregunta
    {
        [Key]
        [Column("reporte_id")]
        public int IdReporte { get; set; }

        [Column("pregunta_id")]
        public int IdPregunta { get; set; }

        [Column("usuario_id")]
        public int IdUsuario { get; set; }

        [Column("motivo_reporte")]
        public string MotivoReporte { get; set; }

        [Column("comentario_adicional")]
        public string? ComentarioAdicional { get; set; }

        [Column("fecha_reporte")]
        public DateTime? FechaReporte { get; set; } // Le pongo ? porque en tu DB permite NULL (aunque tenga default)

        [Column("estado_reporte")]
        public string EstadoReporte { get; set; }
    }
}