using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPersonal.Models
{
    [Table("CATEGORIAS")]
    public class Categoria
    {
        [Key]
        [Column("categoria_id")]
        public int IdCategoria { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; }
    }
}
