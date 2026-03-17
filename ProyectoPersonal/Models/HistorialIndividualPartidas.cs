using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("HISTORIAL_INDIVIDUAL")]
    public class HistorialIndividualPartidas
    {
        [Column("usuario_id")]
        public int IdUsuario { get; set; }
        [Column("cuestionario_id")]
        public int IdCuestionario { get; set; }
        [NotMapped]
        public string NombreCuestionario { get; set; }
        [Column("puntuacion")]
        public int Puntuacion { get; set; }
        [Column("preguntas_correctas")]
        public int PreguntasCorrectas { get; set; }
        [Column("preguntas_totales")]
        public int PreguntasTotales { get; set; }
        [Column("fecha_finalizacion")]
        public DateTime FechaFinalizacion { get; set; }
    }
}
