namespace ProyectoPersonal.Models
{
    public class ParticipantePartida
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public bool HaRespondido { get; set; }
        public int Puntuacion { get; set; }
    }
}
