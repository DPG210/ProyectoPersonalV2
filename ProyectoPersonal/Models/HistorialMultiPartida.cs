namespace ProyectoPersonal.Models
{
    public class HistorialMultiPartida
    {
        
            public int IdPartida { get; set; }
            public string NombreCuestionario { get; set; }
            public DateTime Fecha { get; set; } 
            public string Ganador { get; set; }

            public List<ParticipantePartida> Participantes { get; set; }

            public HistorialMultiPartida()
            {
                this.Participantes = new List<ParticipantePartida>();
            }
        
    }
}
