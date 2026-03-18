using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class CreadorController : Controller
    {
        private IRepositoryCuestionarios repoCuestionarios;
        public CreadorController(IRepositoryCuestionarios repoCuestionarios)
        {
            this.repoCuestionarios = repoCuestionarios;
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> Cuestionarios(string nombreCategoria)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Usuarios"); // O como se llame tu controlador de login
            }
            List<string> cuestionarios = await this.repoCuestionarios.GetCuestionariosAsync(nombreCategoria, idUsuario);
            return View(cuestionarios);
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> CreateCuestionario()
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            List<string> categorias = await this.repoCuestionarios.GetCategoriasAsync();
            return View(categorias);
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CreateCuestionario(string categoria, string titulo, string descripcion, bool esPublico)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario == null)
            {
                return RedirectToAction("Login");
            }
            await this.repoCuestionarios.CreateCuestionarioAsync(categoria, titulo, descripcion, idUsuario, esPublico);
            List<string> categorias = await this.repoCuestionarios.GetCategoriasAsync();
            return View(categorias);
        }
        [HttpGet]
        public async Task<IActionResult> CreatePregunta()
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // 1. Cargamos las categorías (tu código original)
            List<string> categorias = await this.repoCuestionarios.GetCategoriasAsync();

            // 2. Cargamos LOS CUESTIONARIOS DEL USUARIO para el desplegable
            ViewBag.MisCuestionarios = await this.repoCuestionarios.GetCuestionariosUsuarioAsync(idUsuario);

            return View(categorias);
        }

        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CreatePregunta(Pregunta pregunta)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            List<string> categorias = await this.repoCuestionarios.GetCategoriasAsync();
            var misCuestionarios = await this.repoCuestionarios.GetCuestionariosUsuarioAsync(idUsuario);


            bool esMiCuestionario = misCuestionarios.Any(c => c.IdCuestionario == pregunta.IdCuestionario);

            if (!esMiCuestionario)
            {
                // Intento de manipulación de HTML detectado. Lo mandamos fuera.
                TempData["Error"] = "Operación denegada. No puedes alterar el cuestionario de otro ";
                return RedirectToAction("Index", "Trivial");
            }



            await this.repoCuestionarios.InsertPreguntasAsync(pregunta.IdCuestionario, pregunta.Enunciado, pregunta.OpcionCorrecta,
                pregunta.OpcionIncorrecta1, pregunta.OpcionIncorrecta2, pregunta.OpcionIncorrecta3, pregunta.Nivel, pregunta.ExplicacionDidactica);

            // Recargamos el desplegable y mostramos mensaje de éxito
            ViewBag.MisCuestionarios = misCuestionarios;
            ViewData["MensajeExito"] = "¡Pregunta añadida al arsenal con éxito!";

            return View(categorias);
        }
    }
}
