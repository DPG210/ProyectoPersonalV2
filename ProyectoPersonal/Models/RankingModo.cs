using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("ranking_modos")]
    public class RankingModo
    {
        [Key]
        [Column("ranking_id")]
        public int IdRanking { get; set; }

        [Column("usuario_id")]
        public int IdUsuario { get; set; }

        [Column("modo_juego")] // Ej: "Supervivencia", "Ruleta"
        public string ModoJuego { get; set; }

        [Column("cuestionario_id")] // Ej: "MIXTO", "Historia"
        public int IdCuestionario { get; set; }

        [Column("puntos")]
        public int Puntos { get; set; }

        [Column("fecha_partida")]
        public DateTime FechaPartida { get; set; }
        [NotMapped]
        public string NombreCuestionario { get; set; }
    }
}
