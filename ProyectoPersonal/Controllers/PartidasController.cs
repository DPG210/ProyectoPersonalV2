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
    
    public class PartidasController : BaseController
    {
        private readonly IMemoryCache memoryCache;
        private readonly IHubContext<TrivialHub> hubContext;

        private readonly IRepositoryUsuarios repoUsuarios;
        private readonly IRepositorySocial repoSocial;
        private readonly IRepositoryCuestionarios repoCuestionarios;
        private readonly IRepositorySalas repoSalas;
        private readonly IRepositoryJuego repoJuego;

        public PartidasController(
            IMemoryCache memoryCache,
            IHubContext<TrivialHub> hubContext,
            IRepositoryUsuarios repoUsuarios,
            IRepositorySocial repoSocial,
            IRepositoryCuestionarios repoCuestionarios,
            IRepositorySalas repoSalas,
            IRepositoryJuego repoJuego)
        {
            this.memoryCache = memoryCache;
            this.hubContext = hubContext;
            this.repoUsuarios = repoUsuarios;
            this.repoSocial = repoSocial;
            this.repoCuestionarios = repoCuestionarios;
            this.repoSalas = repoSalas;
            this.repoJuego = repoJuego;
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> Index()
        {
            List<string> categorias= await this.repoCuestionarios.GetCategoriasAsync();
            
            int corazones = await this.repoUsuarios.ActualizarYObtenerCorazonesAsync(UsuarioActualId);
                
            ViewBag.Corazones = corazones;
            ViewBag.Invitaciones = await this.repoSocial.GetInvitacionesPendientesAsync(UsuarioActualId);
            ViewBag.SolicitudesAmistad = await this.repoSocial.GetSolicitudesRecibidasAsync(UsuarioActualId);
            
            if (User.IsInRole("ADMIN") || User.IsInRole("Admin"))
            {
                var reportesPendientes = await this.repoCuestionarios.GetReportesAbiertosAsync();
                ViewBag.NumReportes = reportesPendientes.Count;
            }
            
            
            ViewBag.NumSolicitudes = await this.repoSocial.GetNumeroSolicitudesPendientesAsync(UsuarioActualId);
            
            return View(categorias);
        }
        
        
        [AuthorizeUsuario]
        public async Task<IActionResult> PreguntasTrivial(string nombreCuestionario, int cantidad, int tiempo, string codigoSala, int? nivel)
        {
            bool tieneVidas = await this.repoUsuarios.ConsumirCorazonAsync(UsuarioActualId);
            if (!tieneVidas)
            {
                TempData["ErrorVidas"] = "¡Te has quedado sin corazones! Espera un poco o consigue más.";
                return RedirectToAction("Index");
            }

            string cacheKey = "CACHE_IDS_" + nombreCuestionario + "_" + nivel;

            List<int> idsPreguntas;
            if (this.memoryCache.Get(cacheKey) == null)
            {
                idsPreguntas = await this.repoCuestionarios.GetIdsPreguntasAsync(nombreCuestionario,nivel);

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
                var sala = await this.repoSalas.GetSalaPorCodigoAsync(codigoSala);
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
            bool tieneVidas = await this.repoUsuarios.ConsumirCorazonAsync(UsuarioActualId);
            if (!tieneVidas)
            {
                TempData["ErrorVidas"] = "¡Te has quedado sin corazones! Espera un poco o consigue más.";
                return RedirectToAction("Index");
            }

            string cacheKey = "CACHE_IDS_" + nombreCuestionario + "_" + nivel;

            List<int> idsPreguntas;
            if (this.memoryCache.Get(cacheKey) == null)
            {
                idsPreguntas = await this.repoCuestionarios.GetIdsPreguntasAsync(nombreCuestionario, nivel);

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
                var sala = await this.repoSalas.GetSalaPorCodigoAsync(codigoSala);
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
            bool tieneVidas = await this.repoUsuarios.ConsumirCorazonAsync(UsuarioActualId);
            if (!tieneVidas)
            {
                TempData["ErrorVidas"] = "Necesitas al menos 1 corazón para el Modo Supervivencia.";
                return RedirectToAction("Index");
            }

            string cacheKey = "CACHE_IDS_" + nombreCuestionario + "_0" ;

            List<int> idsPreguntas;
            if (this.memoryCache.Get(cacheKey) == null)
            {
                idsPreguntas = await this.repoCuestionarios.GetIdsPreguntasAsync(nombreCuestionario, 0);

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
            bool tieneVidas = await this.repoUsuarios.ConsumirCorazonAsync(UsuarioActualId);
            if (!tieneVidas)
            {
                TempData["ErrorVidas"] = "Necesitas al menos 1 corazón para jugar a la Ruleta.";
                return RedirectToAction("Index");
            }
            List<string> AllTemas = await this.repoCuestionarios.GetAllNombresCuestionariosPublicosAsync(UsuarioActualId);

            Random randomString = new Random();
            string temaFinal = AllTemas[randomString.Next(AllTemas.Count)];


            string cacheKey = "CACHE_IDS_"+temaFinal+"_0";
            
            List<int> idsPreguntas;
            if (this.memoryCache.Get(cacheKey) == null)
            {
                idsPreguntas = await this.repoCuestionarios.GetIdsPreguntasAsync(temaFinal,0);

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
            await this.repoJuego.GuardarRankingModoAsync(UsuarioActualId, modoJuego,puntos, nombreCuestionario);

            TempData["MensajeExito"] = $"¡Partida de Supervivencia terminada! Has conseguido {puntos} puntos.";

            return RedirectToAction("Index","Partidas");
        }
        
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> ValidarPregunta(int idPregunta, string respuestaUsuario)
        {
            string respuestaReal = await this.repoCuestionarios.GetRespuestaCorrectaAsync(idPregunta);

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
