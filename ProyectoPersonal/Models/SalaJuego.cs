namespace ProyectoPersonal.Models
{
    public class SalaJuego
    {
        public int IdSala { get; set; }
        public string CodigoSala { get; set; }
        public string NombreCuestionario { get; set; }
        public string Estado { get; set; }
        public int IdAnfitrion { get; set; }
        public string NombreAnfitrion { get; set; }
        public string TipoJuego { get; set; } 
        public int CantidadPreguntas { get; set; }
        public int Tiempo { get; set; }
        public bool Publica { get; set; }
        public int TotalJugadores { get; set; }
        public int CapacidadMaxima { get; set; }
        public List<ParticipantePartida> Participantes { get; set; }

        public SalaJuego()
        {
            this.Participantes = new List<ParticipantePartida>();
        }
    }
}
