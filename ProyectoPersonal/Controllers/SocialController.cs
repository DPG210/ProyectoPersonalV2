using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class SocialController : BaseController
    {
        private readonly IRepositorySocial repoSocial;
        private readonly IRepositoryCuestionarios repoCuestionarios;

        public SocialController(IRepositorySocial repoSocial, IRepositoryCuestionarios repoCuestionarios)
        {
            this.repoSocial = repoSocial;
            this.repoCuestionarios = repoCuestionarios;
        }

        [AuthorizeUsuario]
        public async Task<IActionResult> EnviarInvitacion(int idDestino)
        {
            await this.repoSocial.EnviarSolicitudAsync(UsuarioActualId, idDestino);

            TempData["TabActiva"] = "social";

            return RedirectToAction("Perfil","Managed");
        }
        [HttpPost]
        [AuthorizeUsuario]
        public async Task<IActionResult> EnviarInvitacionPartida(int idAmigo, string codigoSala)
        {
            try
            {
               
                await this.repoSocial.RegistrarInvitacionAsync(UsuarioActualId, idAmigo, codigoSala);

                
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al enviar" });
            }
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> AceptarAmigo(int idAmigo)
        {
            await this.repoSocial.ResponderSolicitudAsync(idAmigo, UsuarioActualId, "ACEPTADA");

            TempData["TabActiva"] = "social";
            return RedirectToAction("Perfil","Managed");
        }

        [AuthorizeUsuario]
        public async Task<IActionResult> RechazarAmigo(int idAmigo)
        {
            await this.repoSocial.ResponderSolicitudAsync(idAmigo, UsuarioActualId, "RECHAZADA");

            TempData["TabActiva"] = "social";

            return RedirectToAction("Perfil", "Managed");
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<JsonResult> ReportarPregunta(int idPregunta, string motivo, string comentario)
        {
            try
            {
                await this.repoCuestionarios.EnviarReporteAsync(idPregunta, UsuarioActualId, motivo, comentario);
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
            List<VistaReportePregunta> reportes = await this.repoCuestionarios.GetReportesAbiertosAsync();
            return View(reportes);
        }
        [AuthorizeUsuario(Policy = "SoloAdmin")]
        [HttpPost]
        public async Task<IActionResult> CerrarReporte(int idReporte)
        {
            await this.repoCuestionarios.ResolverReporteAsync(idReporte);
            return RedirectToAction("BuzonReportes");
        }
        [HttpPost]
        [AuthorizeUsuario]
        public async Task<IActionResult> ResponderInvitacionPartida(string codigoSala, bool aceptar)
        {

            if (aceptar)
            {
                await this.repoSocial.ActualizarEstadoInvitacionAsync(UsuarioActualId, codigoSala, "aceptada");

                return RedirectToAction("Unirse", "SalasMultijugador", new { codigoSala = codigoSala });
            }
            else
            {
                await this.repoSocial.ActualizarEstadoInvitacionAsync(UsuarioActualId, codigoSala, "rechazada");
                return RedirectToAction("Index", "Partidas"); 
            }
        }
    }
}
