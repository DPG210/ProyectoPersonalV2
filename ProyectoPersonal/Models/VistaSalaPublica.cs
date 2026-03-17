using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("vw_SalasPublicas")]
    public class VistaSalaPublica
    {
        [Column("partida_id")]
        public int IdPartida { get; set; }

        [Column("codigo_sala")]
        public string CodigoSala { get; set; }

        [Column("anfitrion_id")]
        public int IdAnfitrion { get; set; }

        [Column("nombre_anfitrion")]
        public string NombreAnfitrion { get; set; }

        [Column("nombre_cuestionario")]
        public string NombreCuestionario { get; set; }

        [Column("tipo_juego")]
        public string TipoJuego { get; set; }

        [Column("cantidad_preguntas")]
        public int CantidadPreguntas { get; set; }

        [Column("es_publica")]
        public bool EsPublica { get; set; }

        [Column("total_jugadores")]
        public int TotalJugadores { get; set; }

        [Column("tiempo_pregunta")]
        public int TiempoPregunta { get; set; }
    }
}