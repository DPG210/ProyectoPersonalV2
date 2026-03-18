using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class SalasMultijugadorController : Controller
    {
        private readonly IRepositorySalas repoSalas;
        private readonly IRepositoryCuestionarios repoCuestionarios;
        private readonly IRepositorySocial repoSocial;

        public SalasMultijugadorController(
            IRepositorySalas repoSalas,
            IRepositoryCuestionarios repoCuestionarios,
            IRepositorySocial repoSocial)
        {
            this.repoSalas = repoSalas;
            this.repoCuestionarios = repoCuestionarios;
            this.repoSocial = repoSocial;
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CrearSala(string nombreCuestionario, string tipoPartida, int cantidad, int tiempo, bool publico, int capacidad)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            int idCuestionario = await this.repoCuestionarios.GetIdCuestionarioByNombreAsync(nombreCuestionario);
            
            SalaJuego sala = await this.repoSalas.CreateSalaJuegoAsync(idUsuario, idCuestionario, tipoPartida, cantidad, tiempo, publico, capacidad);

            HttpContext.Session.SetString("TIPO_PARTIDA_" + sala.IdSala, tipoPartida);

            return RedirectToAction("Lobby", new { codigo = sala.CodigoSala });
        }

        [AuthorizeUsuario]
        public async Task<IActionResult> Lobby(string codigo)
        {
            SalaJuego sala = await this.repoSalas.GetSalaPorCodigoAsync(codigo);
            if (sala == null) return RedirectToAction("Index", "Home");
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario != null)
            {
                ViewBag.Amigos = await this.repoSocial.GetAmistadesAsync(idUsuario);
            }
            return View(sala);
        }

        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> EmpezarPartida(int idSala, string codigo)
        {
            await this.repoSalas.CambiarEstadoPartidaAsync(idSala, "JUGANDO");

            SalaJuego sala = await this.repoSalas.GetSalaPorCodigoAsync(codigo);

            if (sala.TipoJuego == "quizz") 
            {
                return RedirectToAction("PreguntasQuizz","Partidas", new
                {
                    nombreCuestionario = sala.NombreCuestionario,
                    cantidad = 10,
                    tiempo = 30,
                    codigoSala = codigo
                });
            }
            else
            {
                return RedirectToAction("PreguntasTrivial", "Partidas", new
                {
                    nombreCuestionario = sala.NombreCuestionario,
                    cantidad = sala.CantidadPreguntas,
                    codigoSala = codigo
                });
            }
        }
        [AuthorizeUsuario]
        [HttpGet]
        public async Task<IActionResult> EsperandoInicio(string codigo)
        {
            ViewBag.CodigoSala = codigo;
            return View();
        }
        [AuthorizeUsuario]
        [HttpGet]
        public async Task<IActionResult> Unirse(string codigoSala)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            ParticipantePartida participante = await this.repoSalas.UnirseAPartidaAsync(idUsuario, codigoSala);
            if (participante == null)
            {
                TempData["Error"] = "La sala está llena o ya ha comenzado la partida.";
                return RedirectToAction("Index", "Home");
            }
            
            await this.repoSocial.ActualizarEstadoInvitacionAsync(idUsuario, codigoSala, "aceptada");
            SalaJuego sala = await this.repoSalas.GetSalaPorCodigoAsync(codigoSala);
            int idPartida = sala.IdSala;
            ViewBag.CodigoSala = codigoSala;
            ViewBag.PartidaId = idPartida;
            
            return View("EsperandoInicio");
        }
        [AuthorizeUsuario]
        [HttpGet]
        public async Task<IActionResult> GetEstadoPartidaJson(string codigo)
        {
            if (string.IsNullOrEmpty(codigo))
                return Json(new { estado = "desconocido" });

            var sala = await this.repoSalas.GetSalaPorCodigoAsync(codigo);
            if (sala == null)
                return Json(new { estado = "desconocido" });

            return Json(new
            {
                estado = sala.Estado.ToLower(),
                tipoJuego = sala.TipoJuego,
                cuestionario = sala.NombreCuestionario,
                cantidad = sala.CantidadPreguntas,
                tiempo = sala.Tiempo,
                jugadoresActuales = sala.TotalJugadores,
                capacidadMaxima = sala.CapacidadMaxima
            });
        }
        [AuthorizeUsuario]
        [HttpGet]
        public async Task<IActionResult> BuscadorSalasPublicas()
        {
            List<SalaJuego> salasPublicas = await this.repoSalas.GetSalasPublicasAsync();
            return View(salasPublicas);
        }
        [HttpPost]
        [AuthorizeUsuario]
        public async Task<IActionResult> CancelarSalaManual(int idSala)
        {
            string idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idClaim))
            {
                return RedirectToAction("Index", "Home");
            }

            int miId = int.Parse(idClaim);

            bool cancelada = await this.repoSalas.CancelarSalaAnfitrionAsync(idSala, miId);
            
            if (cancelada)
            {
                TempData["MensajeExito"] = "La sala ha sido cerrada y eliminada del radar.";
            }
            return RedirectToAction("Index", "Partidas"); 
        }
    }
}
