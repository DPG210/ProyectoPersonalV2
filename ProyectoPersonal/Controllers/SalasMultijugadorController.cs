using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;


namespace ProyectoPersonal.Controllers
{
    public class SalasMultijugadorController : BaseController
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
            
            int idCuestionario = await this.repoCuestionarios.GetIdCuestionarioByNombreAsync(nombreCuestionario);
            if(idCuestionario == 0) 
            { 
                return RedirectToAction("Index","Partidas"); 
            }
            SalaJuego sala = await this.repoSalas.CreateSalaJuegoAsync(UsuarioActualId, idCuestionario, tipoPartida, cantidad, tiempo, publico, capacidad);

            HttpContext.Session.SetString("TIPO_PARTIDA_" + sala.IdSala, tipoPartida);

            return RedirectToAction("Lobby", new { codigo = sala.CodigoSala });
        }

        [AuthorizeUsuario]
        public async Task<IActionResult> Lobby(string codigo)
        {
            SalaJuego sala = await this.repoSalas.GetSalaPorCodigoAsync(codigo);
            if (sala == null) return RedirectToAction("Index", "Home");
            
            ViewBag.Amigos = await this.repoSocial.GetAmistadesAsync(UsuarioActualId);
            
            return View(sala);
        }

        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> EmpezarPartida(string codigo)
        {
            SalaJuego sala = await this.repoSalas.GetSalaPorCodigoAsync(codigo);
            if (sala == null)
                return RedirectToAction("Index", "Partidas");
            
            await this.repoSalas.CambiarEstadoPartidaAsync(sala.IdSala, "JUGANDO");

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
            ParticipantePartida participante = await this.repoSalas.UnirseAPartidaAsync(UsuarioActualId, codigoSala);
            if (participante == null)
            {
                TempData["Error"] = "La sala está llena o ya ha comenzado la partida.";
                return RedirectToAction("Index", "Home");
            }
            
            await this.repoSocial.ActualizarEstadoInvitacionAsync(UsuarioActualId, codigoSala, "aceptada");
            SalaJuego sala = await this.repoSalas.GetSalaPorCodigoAsync(codigoSala);
            if (sala == null)
            {
                return RedirectToAction("Index", "Partidas");
            }

            ViewBag.CodigoSala = codigoSala;
            ViewBag.PartidaId = sala.IdSala;

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
            bool cancelada = await this.repoSalas.CancelarSalaAnfitrionAsync(idSala, UsuarioActualId);
            
            if (cancelada)
            {
                TempData["MensajeExito"] = "La sala ha sido cerrada y eliminada del radar.";
            }
            return RedirectToAction("Index", "Partidas"); 
        }
        [HttpPost]
        [AuthorizeUsuario(Policy ="SoloAdmin")]
        public async Task<IActionResult> ForzarCierreSala(int idSala)
        {
            await this.repoSalas.CerrarSalaAdminAsync(idSala);

            TempData["MensajeExito"] = "Sala purgada del sistema correctamente.";

            return RedirectToAction("BuscadorSalasPublicas");
        }
    }
}
