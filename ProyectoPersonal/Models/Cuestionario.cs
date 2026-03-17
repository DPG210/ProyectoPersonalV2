using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("CUESTIONARIOS")]
    public class Cuestionario
    {
        [Key]
        [Column("cuestionario_id")]
        public int IdCuestionario { get; set; }
        [Column("categoria_id")]
        public int IdCategoria { get; set; }
        [Column("titulo")]
        public string Titulo { get; set; }
        [Column("creador_id")]
        public int CreadorId { get; set; }
        [Column("es_publico")]
        public bool EsPublico { get; set; }
    }
}
