using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class SocialController : Controller
    {
        private RepositoryTrivial repo;
        public SocialController (RepositoryTrivial repo)
        {
            this.repo = repo;
        }
        
        [AuthorizeUsuario]
        public async Task<IActionResult> EnviarInvitacion(int idDestino)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            await this.repo.EnviarSolicitudAsync(idUsuario, idDestino);

            TempData["TabActiva"] = "social";

            // Después de enviar, volvemos a la lista de amigos
            return RedirectToAction("Perfil","Managed");
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> AceptarAmigo(int idAmigo)
        {
            // 1. Obtenemos quién soy yo (el receptor que acepta)

            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // 2. Llamamos al repo
            // idAmigo: es el que envió la petición (id_emisor en tu tabla)
            // idLogueado: eres tú, el que la recibe (id_receptor)
            await this.repo.ResponderSolicitudAsync(idAmigo, idUsuario, "ACEPTADA");

            TempData["TabActiva"] = "social";
            // 3. Volvemos a la vista de comunidad/amigos para ver los cambios
            return RedirectToAction("Perfil","Managed");
        }

        // Acción para Rechazar la invitación
        [AuthorizeUsuario]
        public async Task<IActionResult> RechazarAmigo(int idAmigo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Cambiamos el estado a RECHAZADA (o podrías borrar la fila si prefieres)
            await this.repo.ResponderSolicitudAsync(idAmigo, idUsuario, "RECHAZADA");

            TempData["TabActiva"] = "social";

            return RedirectToAction("Perfil", "Managed");
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<JsonResult> ReportarPregunta(int idPregunta, string motivo, string comentario)
        {

            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Si la sesión caducó o es invitado
            if (idUsuario == null)
            {
                return Json(new { success = false, message = "Debes iniciar sesión para reportar." });
            }

            try
            {
                await this.repo.EnviarReporteAsync(idPregunta, idUsuario, motivo, comentario);
                return Json(new { success = true, message = "Reporte enviado correctamente. ¡Gracias por tu ayuda!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al enviar el reporte." });
            }
        }
        [AuthorizeUsuario(Policy = "SoloAdmin")]
        [HttpGet]
        public async Task<IActionResult> BuzonReportes()
        {
            List<VistaReportePregunta> reportes = await this.repo.GetReportesAbiertosAsync();
            return View(reportes);
        }
        [AuthorizeUsuario(Policy = "SoloAdmin")]
        [HttpPost]
        public async Task<IActionResult> CerrarReporte(int idReporte)
        {
            await this.repo.ResolverReporteAsync(idReporte);
            return RedirectToAction("BuzonReportes");
        }
    }
}
