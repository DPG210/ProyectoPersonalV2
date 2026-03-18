using Humanizer;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Hubs;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.IO.Pipelines;
using System.Security.Claims;
using static Azure.Core.HttpHeader;


namespace ProyectoPersonal.Controllers
    {
        public class DatosController : Controller
        {
            
            private readonly IRepositoryUsuarios repoUsuarios;
            private readonly IRepositorySocial repoSocial;
            private readonly IRepositorySalas repoSalas;
            private readonly IRepositoryCuestionarios repoCuestionarios;
            private readonly IRepositoryJuego repoJuego;

            private readonly IMemoryCache memoryCache;
            private readonly IHubContext<TrivialHub> hubContext;

            public DatosController(
                IRepositoryUsuarios repoUsuarios,
                IRepositorySocial repoSocial,
                IRepositorySalas repoSalas,
                IRepositoryCuestionarios repoCuestionarios,
                IRepositoryJuego repoJuego,
                IMemoryCache memoryCache,
                IHubContext<TrivialHub> hubContext)
            {
                this.repoUsuarios = repoUsuarios;
                this.repoSocial = repoSocial;
                this.repoSalas = repoSalas;
                this.repoCuestionarios = repoCuestionarios;
                this.repoJuego = repoJuego;
                this.memoryCache = memoryCache;
                this.hubContext = hubContext;
            }
            [AuthorizeUsuario]
            public async Task<JsonResult> GetCuestionariosJson(string nombreCategoria)
            {
                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var cuestionarios = await this.repoCuestionarios.GetCuestionariosAsync(nombreCategoria, idUsuario);
                return Json(cuestionarios);
            }
            [AuthorizeUsuario]
            public async Task<JsonResult> GetCuestionariosJsonCompletos(string nombreCategoria)
            {
                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var cuestionarios = await this.repoCuestionarios.GetCuestionarioCompletoAsync(nombreCategoria, idUsuario);
                return Json(cuestionarios);
            }
            [AuthorizeUsuario]
            public async Task<JsonResult> ActualizarParticipantes(string codigoPartida)
            {
                var participantes = await this.repoSalas.GetParticipantesSalaAsync(codigoPartida);
                return Json(participantes);
            }

            [AuthorizeUsuario]
            [HttpGet]
            public async Task<JsonResult> GetEstadoSala(string codigo)
            {
                var sala = await this.repoSalas.GetSalaPorCodigoAsync(codigo);
                return Json(new { estado = sala?.Estado });
            }
            [AuthorizeUsuario]
            [HttpPost]
            public async Task<JsonResult> EnviarInvitacionPartida(string nombreAmigo, string codigoSala)
            {
                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);


                int idAmigo = await this.repoUsuarios.GetIdUsuarioByNombreAsync(nombreAmigo);

                // 2. Insertamos en la tabla de invitaciones (necesitas crear este método en el repo)
                await this.repoSocial.RegistrarInvitacionAsync(idUsuario, idAmigo, codigoSala);

                return Json(new { status = "OK" });
            }

            [AuthorizeUsuario]
            [HttpPost]
            public async Task<JsonResult> RegistrarRespuesta(int partidaId, int indicePregunta, bool esCorrecta)
            {
                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                int puntos = esCorrecta ? 1 : 0;
                await this.repoJuego.RegistrarRespuestaAsync(partidaId, idUsuario, indicePregunta, esCorrecta, puntos);

                bool todosListos = await this.repoJuego.TodosHanRespondidoAsync(partidaId, indicePregunta);
                var ranking = await this.repoJuego.GetRankingAsync(partidaId);

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
                await this.repoSalas.FinalizarPartidaMultijugadorAsync(partidaId);
                var ranking = await this.repoJuego.GetRankingAsync(partidaId);
                return Json(ranking);
            }
            [AuthorizeUsuario]
            [HttpPost]
            public async Task<JsonResult> AvanzarPregunta(int partidaId, int nuevoIndice)
            {
                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                bool todosRespondieron = await this.repoJuego.TodosHanRespondidoAsync(partidaId, nuevoIndice - 1);
                if (!todosRespondieron)
                    return Json(new { todosAvanzaron = false });

                await this.repoJuego.AvanzarJugadorAsync(partidaId, idUsuario, nuevoIndice);
                bool todosAvanzaron = await this.repoJuego.TodosHanRespondidoAsync(partidaId, nuevoIndice);
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

                int puntuacion = correctas * 10;

                await this.repoJuego.GuardarHistorialIndividualAsync(idUsuario, cuestionario, puntuacion, correctas, totales);

                return Json(new { success = true });
            }


            [AuthorizeUsuario]
            [HttpGet]
            public async Task<JsonResult> ObtenerPreguntaPorId(int idPregunta)
            {
                Pregunta p = await this.repoCuestionarios.GetPreguntaByIdAsync(idPregunta);

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
                string cacheKey = $"RANKING_{modo}_{filtro}_{orden}";

                if (!this.memoryCache.TryGetValue(cacheKey, out List<dynamic> listaRankings))
                {
                    listaRankings = await this.repoJuego.GetTopRankingGlobalAsync(modo, filtro, orden);

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

                List<Usuario> usuarios = await this.repoSocial.BuscarUsuariosPorNombreAsync(textoBusqueda, miId);

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


