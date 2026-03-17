using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("PREGUNTAS")]
    public class Pregunta
    {
        [Key]
        [Column("pregunta_id")]
        public int IdPregunta { get; set; }
        [Column("cuestionario_id")]
        public int IdCuestionario { get; set; }
        [Column("enunciado")]
        public string Enunciado { get; set; }
        [Column("explicacion_didactica")]
        public string ExplicacionDidactica { get; set; }
        [Column("respuesta_correcta")]
        public string OpcionCorrecta {  get; set; }
        [Column("opcion_incorrecta1")]
        public string OpcionIncorrecta1 { get; set; }
        [Column("opcion_incorrecta2")]
        public string OpcionIncorrecta2 { get; set; }
        [Column("opcion_incorrecta3")]
        public string OpcionIncorrecta3 { get; set; }
        [Column("nivel")]
        public int Nivel { get; set; }
    }
}

