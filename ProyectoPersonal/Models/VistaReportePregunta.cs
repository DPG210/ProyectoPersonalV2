namespace ProyectoPersonal.Models
{
    public class VistaReportePregunta
    {
        public int IdReporte { get; set; }
        public int IdPregunta { get; set; }
        public string EnunciadoPregunta { get; set; } // El texto de la pregunta reportada
        public string NombreUsuario { get; set; }     // Quién se ha quejado
        public string Motivo { get; set; }
        public string? Comentario { get; set; }
        public DateTime? Fecha { get; set; }
        public string Estado { get; set; }
    }
}
