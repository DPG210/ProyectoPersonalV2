using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("PARTICIPANTES_PARTIDA")]
    public class ParticipanteEntidad
    {
        [Column("partida_id")]
        public int IdPartida { get; set; }

        [Column("usuario_id")]
        public int IdUsuario { get; set; }

        [Column("puntuacion_actual")]
        public int PuntuacionActual { get; set; }

        [Column("ha_terminado")]
        public bool HaTerminado { get; set; }

        [Column("indice_respondido")]
        public int IndiceRespondido { get; set; }

        [Column("es_ganador")]
        public bool? EsGanador { get; set; }
    }
}
