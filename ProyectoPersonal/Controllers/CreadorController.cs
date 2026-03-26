using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class CreadorController : BaseController
    {
        private IRepositoryCuestionarios repoCuestionarios;
        public CreadorController(IRepositoryCuestionarios repoCuestionarios)
        {
            this.repoCuestionarios = repoCuestionarios;
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> Cuestionarios(string nombreCategoria)
        {
            List<string> cuestionarios = await this.repoCuestionarios.GetCuestionariosAsync(nombreCategoria, UsuarioActualId);
            return View(cuestionarios);
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> CreateCuestionario()
        {
            List<string> categorias = await this.repoCuestionarios.GetCategoriasAsync();
            return View(categorias);
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CreateCuestionario(string categoria, string titulo, string descripcion, bool esPublico)
        {
            await this.repoCuestionarios.CreateCuestionarioAsync(categoria, titulo, descripcion, UsuarioActualId, esPublico);
            List<string> categorias = await this.repoCuestionarios.GetCategoriasAsync();
            return View(categorias);
        }
        [HttpGet]
        public async Task<IActionResult> CreatePregunta()
        {
            List<string> categorias = await this.repoCuestionarios.GetCategoriasAsync();

            ViewBag.MisCuestionarios = await this.repoCuestionarios.GetCuestionariosUsuarioAsync(UsuarioActualId);

            return View(categorias);
        }

        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CreatePregunta(Pregunta pregunta)
        {
            List<string> categorias = await this.repoCuestionarios.GetCategoriasAsync();
            var misCuestionarios = await this.repoCuestionarios.GetCuestionariosUsuarioAsync(UsuarioActualId);

            bool esMiCuestionario = misCuestionarios.Any(c => c.IdCuestionario == pregunta.IdCuestionario);

            if (!esMiCuestionario)
            {
                TempData["Error"] = "Operación denegada. No puedes alterar el cuestionario de otro ";
                return RedirectToAction("Index", "Partidas");
            }

            await this.repoCuestionarios.InsertPreguntasAsync(pregunta.IdCuestionario, pregunta.Enunciado, pregunta.OpcionCorrecta,
                pregunta.OpcionIncorrecta1, pregunta.OpcionIncorrecta2, pregunta.OpcionIncorrecta3, pregunta.Nivel, pregunta.ExplicacionDidactica);

            ViewBag.MisCuestionarios = misCuestionarios;
            ViewData["MensajeExito"] = "¡Pregunta añadida al arsenal con éxito!";

            return View(categorias);
        }
    }
}
