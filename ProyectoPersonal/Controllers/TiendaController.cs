using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Repositories;
using Stripe.Checkout;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class TiendaController : Controller
    {
        private readonly IRepositoryUsuarios repoUsuarios;

        public TiendaController(IRepositoryUsuarios repoUsuarios)
        {
            this.repoUsuarios = repoUsuarios;
        }
        [AuthorizeUsuario]
        public IActionResult Index()
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return View();
        }
        [AuthorizeUsuario]
        [HttpPost]
        public IActionResult ProcesarSuscripcion(string tipoPlan)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var domain = "https://localhost:7113";

            string stripePriceId = "";
            if (tipoPlan == "mensual")
            {
                stripePriceId = "price_1TABSSGsCKiO0dM2SlBKRVR3";
            }
            else if (tipoPlan == "anual")
            {
                stripePriceId = "price_1TABStGsCKiO0dM2MOqu114H";
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                Price = stripePriceId,
                Quantity = 1,
            },
        },
                Mode = "subscription",
                SuccessUrl = domain + "/Tienda/SuscripcionCompletada?session_id={CHECKOUT_SESSION_ID}&plan=" + tipoPlan,
                CancelUrl = domain + "/Tienda/PagoCancelado",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> SuscripcionCompletada(string session_id, string plan)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario != null)
            {
                
                await this.repoUsuarios.RegistrarPagoYActivarVipAsync(idUsuario,session_id, plan);
                TempData["MensajeTienda"] = "¡Bienvenido al club VIP! Ya tienes ventajas exclusivas.";
            }

            return RedirectToAction("Perfil", "Managed", new { id = idUsuario });
        }
        [AuthorizeUsuario]
        public IActionResult PagoCancelado()
        {
            TempData["ErrorTienda"] = "El proceso de pago ha sido cancelado. ¡No te hemos cobrado nada!";
            return RedirectToAction("Index","Partidas");
        }
    }

}
