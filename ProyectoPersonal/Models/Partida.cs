using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("PARTIDAS")]
    public class Partida
    {
        [Key]
        [Column("partida_id")]
        public int IdPartida { get; set; }

        [Column("anfitrion_id")]
        public int IdAnfitrion { get; set; }

        [Column("cuestionario_id")]
        public int IdCuestionario { get; set; }

        [Column("codigo_sala")]
        public string CodigoSala { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("tipo_juego")]
        public string TipoJuego { get; set; }

        [Column("cantidad_preguntas")]
        public int CantidadPreguntas { get; set; }

        [Column("tiempo_pregunta")]
        public int TiempoPregunta { get; set; }

        [Column("es_publica")]
        public bool EsPublica { get; set; }
        [Column("capacidad_maxima")]
        public int CapacidadMaxima { get; set; }
    }
}