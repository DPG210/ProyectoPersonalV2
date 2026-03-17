using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class SalasMultijugadorController : Controller
    {
        private RepositoryTrivial repo;
        public SalasMultijugadorController(RepositoryTrivial repo)
        {
            this.repo = repo;
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CrearSala(string nombreCuestionario, string tipoPartida, int cantidad, int tiempo, bool publico, int capacidad)
        {
            // Supongamos que tenemos el ID del usuario en la sesión
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            int idCuestionario = await this.repo.GetIdCuestionarioByNombreAsync(nombreCuestionario);
            // Creamos la sala y el repo ya nos devuelve el objeto SalaJuego completo
            SalaJuego sala = await this.repo.CreateSalaJuegoAsync(idUsuario, idCuestionario, tipoPartida, cantidad, tiempo, publico, capacidad);

            HttpContext.Session.SetString("TIPO_PARTIDA_" + sala.IdSala, tipoPartida);

            return RedirectToAction("Lobby", new { codigo = sala.CodigoSala });
        }

        [AuthorizeUsuario]
        public async Task<IActionResult> Lobby(string codigo)
        {
            SalaJuego sala = await this.repo.GetSalaPorCodigoAsync(codigo);
            if (sala == null) return RedirectToAction("Index", "Home");
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario != null)
            {
                // Cargamos la lista de amigos en el ViewBag para que la vista la recorra
                // Es la misma línea que usas en el método Amigos
                ViewBag.Amigos = await this.repo.GetAmistadesAsync(idUsuario);
            }
            return View(sala);
        }

        // 3. EL BOTÓN "EMPEZAR" DEL HOST
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> EmpezarPartida(int idSala, string codigo)
        {
            // Cambiamos el estado en la DB de 'LOBBY' a 'JUGANDO'
            await this.repo.CambiarEstadoPartidaAsync(idSala, "JUGANDO");

            SalaJuego sala = await this.repo.GetSalaPorCodigoAsync(codigo);

            if (sala.TipoJuego == "quizz") // Asegúrate de guardar el tipo en la DB al crear la sala
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
            // Pasamos el código a la vista para que el JS sepa qué sala vigilar
            ViewBag.CodigoSala = codigo;
            return View();
        }
        [AuthorizeUsuario]
        [HttpGet]
        public async Task<IActionResult> Unirse(string codigoSala)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // 1. Metemos al usuario en la tabla de participantes (tu lógica actual)
            ParticipantePartida participante = await this.repo.UnirseAPartidaAsync(idUsuario, codigoSala);
            if (participante == null)
            {
                // Mandamos un mensaje de error si intentó entrar a una sala llena
                TempData["Error"] = "La sala está llena o ya ha comenzado la partida.";
                return RedirectToAction("Index", "Home");
            }
            // 2. NUEVO: Marcamos la invitación como 'aceptada' para que el aviso desaparezca
            // Esto es importante para que el "Polling" del Layout deje de mostrarle el Toast
            await this.repo.ActualizarEstadoInvitacionAsync(idUsuario, codigoSala, "aceptada");
            SalaJuego sala = await this.repo.GetSalaPorCodigoAsync(codigoSala);
            int idPartida = sala.IdSala;
            ViewBag.CodigoSala = codigoSala;
            ViewBag.PartidaId = idPartida;
            // 3. Lo mandamos a la vista de espera (tu lógica actual)
            return View("EsperandoInicio");
        }
        [AuthorizeUsuario]
        [HttpGet]
        public async Task<IActionResult> GetEstadoPartidaJson(string codigo)
        {
            if (string.IsNullOrEmpty(codigo))
                return Json(new { estado = "desconocido" });

            var sala = await this.repo.GetSalaPorCodigoAsync(codigo);
            if (sala == null)
                return Json(new { estado = "desconocido" });
            //var sala = await this.repo.GetSalaPorCodigoAsync(codigo);
            //if (sala == null) return NotFound();

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
            List<SalaJuego> salasPublicas = await this.repo.GetSalasPublicasAsync();
            return View(salasPublicas);
        }
    }
}
