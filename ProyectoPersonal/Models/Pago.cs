using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("PAGOS")]
public class Pago
{
    [Key]
    [Column("id_pago")]
    public int IdPago { get; set; }

    [Column("usuario_id")]
    public int IdUsuario { get; set; }

    [Column("stripe_session_id")]
    public string StripeSessionId { get; set; }

    [Column("monto")]
    public decimal Monto { get; set; }

    [Column("plan_tipo")]
    public string PlanTipo { get; set; }

    [Column("fecha_pago")]
    public DateTime FechaPago { get; set; }
}