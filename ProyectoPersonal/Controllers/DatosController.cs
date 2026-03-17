using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Hubs;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class DatosController : Controller
    {
        private RepositoryTrivial repo;
        private IMemoryCache memoryCache;
        private readonly IHubContext<TrivialHub> hubContext;
        public DatosController(RepositoryTrivial repo, IMemoryCache memoryCache, IHubContext<TrivialHub> hubContext)
        {
            this.repo = repo;
            this.memoryCache = memoryCache;
            this.hubContext = hubContext;
        }
        [AuthorizeUsuario]
        public async Task<JsonResult> GetCuestionariosJson(string nombreCategoria)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var cuestionarios = await this.repo.GetCuestionariosAsync(nombreCategoria, idUsuario);
            return Json(cuestionarios);
        }
        [AuthorizeUsuario]
        public async Task<JsonResult> GetCuestionariosJsonCompletos(string nombreCategoria)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cuestionarios = await this.repo.GetCuestionarioCompletoAsync(nombreCategoria, idUsuario);
            return Json(cuestionarios);
        }
        [AuthorizeUsuario]
        public async Task<JsonResult> ActualizarParticipantes(string codigoPartida)
        {
            var participantes = await this.repo.GetParticipantesSalaAsync(codigoPartida);
            return Json(participantes);
        }

        [AuthorizeUsuario]
        [HttpGet]
        public async Task<JsonResult> GetEstadoSala(string codigo)
        {
            var sala = await this.repo.GetSalaPorCodigoAsync(codigo);
            return Json(new { estado = sala?.Estado });
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<JsonResult> EnviarInvitacionPartida(string nombreAmigo, string codigoSala)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);


            int idAmigo = await this.repo.GetIdUsuarioByNombreAsync(nombreAmigo);

            // 2. Insertamos en la tabla de invitaciones (necesitas crear este método en el repo)
            await this.repo.RegistrarInvitacionAsync(idUsuario, idAmigo, codigoSala);

            return Json(new { status = "OK" });
        }

        [AuthorizeUsuario]
        [HttpPost]
        public async Task<JsonResult> RegistrarRespuesta(int partidaId, int indicePregunta, bool esCorrecta)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            int puntos = esCorrecta ? 1 : 0;
            await this.repo.RegistrarRespuestaAsync(partidaId, idUsuario, indicePregunta, esCorrecta, puntos);

            bool todosListos = await this.repo.TodosHanRespondidoAsync(partidaId, indicePregunta);
            var ranking = await this.repo.GetRankingAsync(partidaId);

            if (todosListos)
            {
                await this.hubContext.Clients.Group(partidaId.ToString()).SendAsync("TodosListos", ranking);
            }
            else
            {
                await this.hubContext.Clients.Group(partidaId.ToString()).SendAsync("JugadorRespondio");
            }

            return Json(new { todosListos = todosListos, ranking = ranking });
        }

        [AuthorizeUsuario]
        [HttpGet]
        public async Task<JsonResult> GetRankingFinal(int partidaId)
        {
            await this.repo.FinalizarPartidaMultijugadorAsync(partidaId);
            var ranking = await this.repo.GetRankingAsync(partidaId);
            return Json(ranking);
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<JsonResult> AvanzarPregunta(int partidaId, int nuevoIndice)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // ✅ Primero comprobamos que todos respondieron la pregunta anterior
            bool todosRespondieron = await this.repo.TodosHanRespondidoAsync(partidaId, nuevoIndice - 1);
            if (!todosRespondieron)
                return Json(new { todosAvanzaron = false });

            await this.repo.AvanzarJugadorAsync(partidaId, idUsuario, nuevoIndice);
            bool todosAvanzaron = await this.repo.TodosHanRespondidoAsync(partidaId, nuevoIndice);
            if (todosAvanzaron)
            {

                await this.hubContext.Clients.Group(partidaId.ToString()).SendAsync("TodosAvanzaron");
            }
            return Json(new { todosAvanzaron });
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<JsonResult> GuardarPartidaIndividual(string cuestionario, int correctas, int totales)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Calculamos la puntuación base (ejemplo: 10 puntos por acierto)
            int puntuacion = correctas * 10;

            await this.repo.GuardarHistorialIndividualAsync(idUsuario, cuestionario, puntuacion, correctas, totales);

            return Json(new { success = true });
        }


        [AuthorizeUsuario]
        [HttpGet]
        public async Task<JsonResult> ObtenerPreguntaPorId(int idPregunta)
        {
            Pregunta p = await this.repo.GetPreguntaByIdAsync(idPregunta);

            if (p == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                idPregunta = p.IdPregunta,
                nivel = p.Nivel,
                enunciado = p.Enunciado,
                explicacion = p.ExplicacionDidactica,
                opcionCorrecta = p.OpcionCorrecta,
                opcionIncorrecta1 = p.OpcionIncorrecta1,
                opcionIncorrecta2 = p.OpcionIncorrecta2,
                opcionIncorrecta3 = p.OpcionIncorrecta3
            });
        }

        [AuthorizeUsuario]
        [HttpGet]
        public async Task<JsonResult> GetTopRankings(string modo, string filtro = "", string orden = "desc")
        {
            // Creamos una clave única para cada combinación de filtro
            string cacheKey = $"RANKING_{modo}_{filtro}_{orden}";

            // Intentamos buscarlo en la caché
            if (!this.memoryCache.TryGetValue(cacheKey, out List<dynamic> listaRankings))
            {
                // Si no está (o caducó), lo buscamos en la base de datos
                listaRankings = await this.repo.GetTopRankingGlobalAsync(modo, filtro, orden);

                // Lo guardamos en caché durante 5 minutos
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                this.memoryCache.Set(cacheKey, listaRankings, cacheOptions);
            }

            return Json(listaRankings);
        }
        [HttpGet]
        public async Task<JsonResult> BuscarUsuarios(string textoBusqueda)
        {
            if (string.IsNullOrWhiteSpace(textoBusqueda) || textoBusqueda.Length < 3)
            {
                return Json(new List<Usuario>());
            }

            string idClaim = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            int miId = int.Parse(idClaim);

            List<Usuario> usuarios = await this.repo.BuscarUsuariosPorNombreAsync(textoBusqueda, miId);

            var resultados = usuarios.Select(u => new
            {
                idUsuario = u.IdUsuario,
                nombre = u.Nombre,
                avatar = u.Avatar ?? "avatar1.png"
            });

            return Json(resultados);
        }
    }
}
