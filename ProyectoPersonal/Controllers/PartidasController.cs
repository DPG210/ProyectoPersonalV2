using MailKit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Helpers;
using ProyectoPersonal.Hubs;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using static Azure.Core.HttpHeader;
using static ProyectoPersonal.Models.HistorialMultiPartida;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ProyectoPersonal.Controllers
{
    
    public class PartidasController : Controller
    {
        private IMemoryCache memoryCache;
        private RepositoryTrivial repo;
       
        private readonly IHubContext<TrivialHub> hubContext;
        public PartidasController(RepositoryTrivial repo,  IMemoryCache memoryCache, IHubContext<TrivialHub> hubContext)
        {
            this.repo= repo;
            
            this.memoryCache = memoryCache;
            this.hubContext = hubContext;
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> Index()
        {
            List<string> categorias= await this.repo.GetCategoriasAsync();
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            int corazones = await this.repo.ActualizarYObtenerCorazonesAsync(idUsuario);
                
            ViewBag.Corazones = corazones;
            ViewBag.Invitaciones = await this.repo.GetInvitacionesPendientesAsync(idUsuario);
            ViewBag.SolicitudesAmistad = await this.repo.GetSolicitudesRecibidasAsync(idUsuario);
            
            if (User.IsInRole("ADMIN") || User.IsInRole("Admin"))
            {
                var reportesPendientes = await this.repo.GetReportesAbiertosAsync();
                ViewBag.NumReportes = reportesPendientes.Count;
            }
            
            
            ViewBag.NumSolicitudes = await this.repo.GetNumeroSolicitudesPendientesAsync(idUsuario);
            
            return View(categorias);
        }
        
        
        [AuthorizeUsuario]
        public async Task<IActionResult> PreguntasTrivial(string nombreCuestionario, int cantidad, int tiempo, string codigoSala, int? nivel)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario == null) return RedirectToAction("Login", "Usuarios");

            bool tieneVidas = await this.repo.ConsumirCorazonAsync(idUsuario);
            if (!tieneVidas)
            {
                TempData["ErrorVidas"] = "¡Te has quedado sin corazones! Espera un poco o consigue más.";
                return RedirectToAction("Index");
            }

            string cacheKey = "CACHE_IDS_" + nombreCuestionario + "_" + nivel;

            List<int> idsPreguntas;
            if (this.memoryCache.Get(cacheKey) == null)
            {
                idsPreguntas = await this.repo.GetIdsPreguntasAsync(nombreCuestionario,nivel);

                this.memoryCache.Set(cacheKey, idsPreguntas, TimeSpan.FromHours(2));
            }
            else
            {
                idsPreguntas = this.memoryCache.Get<List<int>>(cacheKey);
            }
            
            Random random;
            if (string.IsNullOrEmpty(codigoSala))
            {
                random = new Random(); 
                ViewBag.PartidaId = 0;
            }
            else
            {
                
                random = new Random(codigoSala.GetHashCode());
                var sala = await this.repo.GetSalaPorCodigoAsync(codigoSala);
                ViewBag.PartidaId = sala?.IdSala ?? 0;
            }

            var idsMezclados = idsPreguntas.OrderBy(p => random.Next()).ToList();

            if (cantidad > 0)
            {
                idsMezclados = idsMezclados.Take(cantidad).ToList();
            }

            ViewBag.Tiempo = tiempo;
            ViewBag.CodigoSala = codigoSala;

            return View(idsMezclados);
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> PreguntasQuizz(string nombreCuestionario, int cantidad, int tiempo, string codigoSala,int? nivel)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario == null) return RedirectToAction("Login", "Usuarios");

            bool tieneVidas = await this.repo.ConsumirCorazonAsync(idUsuario);
            if (!tieneVidas)
            {
                TempData["ErrorVidas"] = "¡Te has quedado sin corazones! Espera un poco o consigue más.";
                return RedirectToAction("Index");
            }

            string cacheKey = "CACHE_IDS_" + nombreCuestionario + "_" + nivel;

            List<int> idsPreguntas;
            if (this.memoryCache.Get(cacheKey) == null)
            {
                idsPreguntas = await this.repo.GetIdsPreguntasAsync(nombreCuestionario, nivel);

                this.memoryCache.Set(cacheKey, idsPreguntas, TimeSpan.FromHours(2));
            }
            else
            {
                idsPreguntas = this.memoryCache.Get<List<int>>(cacheKey);
            }

            Random random;
            if (string.IsNullOrEmpty(codigoSala))
            {
                random = new Random();
                ViewBag.PartidaId = 0;
            }
            else
            {
                random = new Random(codigoSala.GetHashCode());
                var sala = await this.repo.GetSalaPorCodigoAsync(codigoSala);
                ViewBag.PartidaId = sala?.IdSala ?? 0;
            }

            var idsMezclados = idsPreguntas.OrderBy(p => random.Next()).ToList();

            if (cantidad > 0)
            {
                idsMezclados = idsMezclados.Take(cantidad).ToList();
            }

            ViewBag.Tiempo = tiempo;
            ViewBag.CodigoSala = codigoSala;

            return View(idsMezclados);
        }
        
        
        
        [AuthorizeUsuario]
        public async Task<IActionResult> ModoSupervivencia(string nombreCuestionario)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario == null) return RedirectToAction("Login", "Usuarios");

            bool tieneVidas = await this.repo.ConsumirCorazonAsync(idUsuario);
            if (!tieneVidas)
            {
                TempData["ErrorVidas"] = "Necesitas al menos 1 corazón para el Modo Supervivencia.";
                return RedirectToAction("Index");
            }

            string cacheKey = "CACHE_IDS_" + nombreCuestionario + "_0" ;

            List<int> idsPreguntas;
            if (this.memoryCache.Get(cacheKey) == null)
            {
                idsPreguntas = await this.repo.GetIdsPreguntasAsync(nombreCuestionario, 0);

                this.memoryCache.Set(cacheKey, idsPreguntas, TimeSpan.FromHours(2));
            }
            else
            {
                idsPreguntas = this.memoryCache.Get<List<int>>(cacheKey);
            }
            Random random = new Random();
            var idsMezclados = idsPreguntas.OrderBy(p => random.Next()).ToList();

            ViewBag.Modo = "Supervivencia";
            ViewBag.VidasSupervivencia = 3;
            ViewBag.NombreCuestionario = nombreCuestionario;

            return View(idsMezclados);
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> RuletaDeLaMuerte()
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario == null) return RedirectToAction("Login", "Usuarios");

            bool tieneVidas = await this.repo.ConsumirCorazonAsync(idUsuario);
            if (!tieneVidas)
            {
                TempData["ErrorVidas"] = "Necesitas al menos 1 corazón para jugar a la Ruleta.";
                return RedirectToAction("Index");
            }
            List<string> AllTemas = await this.repo.GetAllNombresCuestionariosPublicosAsync(idUsuario);

            Random randomString = new Random();
            string temaFinal = AllTemas[randomString.Next(AllTemas.Count)];


            string cacheKey = "CACHE_IDS_+"+temaFinal+"_0";
            
            List<int> idsPreguntas;
            if (this.memoryCache.Get(cacheKey) == null)
            {
                idsPreguntas = await this.repo.GetIdsPreguntasAsync(temaFinal,0);

                this.memoryCache.Set(cacheKey, idsPreguntas, TimeSpan.FromHours(2));
            }
            else
            {
                idsPreguntas = this.memoryCache.Get<List<int>>(cacheKey);
            }
            Random random = new Random();
            var idsMezclados = idsPreguntas.OrderBy(p => random.Next()).ToList();

            ViewBag.Modo = "RULETA";
            ViewBag.VidasRuleta = 3;
            ViewBag.NombreCuestionario = temaFinal;

            return View(idsMezclados);
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> GuardarRanking(int puntos,string modoJuego, string nombreCuestionario)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Managed");
            }

            await this.repo.GuardarRankingModoAsync(idUsuario, modoJuego,puntos, nombreCuestionario);

            TempData["MensajeExito"] = $"¡Partida de Supervivencia terminada! Has conseguido {puntos} puntos.";

            return RedirectToAction("Index","Partidas");
        }
        
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> ValidarPregunta(int idPregunta, string respuestaUsuario)
        {
            string respuestaReal = await this.repo.GetRespuestaCorrectaAsync(idPregunta);

            string respuestaUsuarioLimpia = respuestaUsuario.SimplifyForTrivia();
            string respuestaRealLimpia = respuestaReal.SimplifyForTrivia();

            bool esCorrecto = HelperComparadorRespuestas.EsRespuestaValida(respuestaUsuarioLimpia, respuestaRealLimpia);

            if (esCorrecto)
            {
                return Json(new
                {
                    status = "SUCCESS",
                    mensaje = "¡Respuesta válida!",
                    esCorrecto = true
                });
            }
            else
            {
                return Json(new
                {
                    status = "REVISION",
                    esCorrecto = false,
                    respuestaCorrecta = respuestaReal,
                    mensaje = "El sistema no reconoce la respuesta, ¿quieres validarla manualmente?"
                });
                
            }
        }
        
    }
}
