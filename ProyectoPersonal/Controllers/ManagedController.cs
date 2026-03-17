using MailKit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Helpers;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProyectoPersonal.Controllers
{
    public class ManagedController : Controller
    {
        private RepositoryTrivial repo;
        private readonly MailKitService mailService;
        public ManagedController(RepositoryTrivial repo, MailKitService mailService)
        {
            this.repo = repo;
            this.mailService = mailService;
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            Usuario usuario = await this.repo.LoginUsuarioAsync(username,password);
            if (usuario != null)
            {
                ClaimsIdentity identity =
                    new ClaimsIdentity(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        ClaimTypes.Name, ClaimTypes.Role);
                Claim claimName =
                    new Claim(ClaimTypes.Name, usuario.Nombre);
                identity.AddClaim(claimName);
                Claim claimId =
                    new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString());
                identity.AddClaim(claimId);
                Claim claimRole =
                    new Claim(ClaimTypes.Role, usuario.NombreRol);
                identity.AddClaim(claimRole);
                Claim claimAvatar = new Claim("Avatar", usuario.Avatar ?? "avatar1.png");
                identity.AddClaim(claimAvatar);

                ClaimsPrincipal userPrincipal =
                    new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync
                    (CookieAuthenticationDefaults.AuthenticationScheme,
                    userPrincipal);
            }
                TempData.Clear();
                return RedirectToAction("Index", "Trivial");
            
            
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync
                (CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Managed");
        }

        public IActionResult ErrorAcceso()
        {
            return View();
        }
        public async Task<IActionResult> CrearUsuario()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearUsuario(Usuario newusuario)
        {
            ModelState.Remove("NombreRol");
            ModelState.Remove("Avatar");
            ModelState.Remove("TokenMail");
            ModelState.Remove("IdUsuario");
            ModelState.Remove("RolId");
            ModelState.Remove("Activo");
            ModelState.Remove("CorazonesActuales");
            ModelState.Remove("CorazonesMaximos");
            ModelState.Remove("AnunciosVistosHoy");

            // 2. Ahora sí, el ModelState debería estar "limpio" solo con Nombre, Email y Password
            if (!ModelState.IsValid)
            {
                // Opcional: Si sigue fallando, pon un breakpoint aquí y mira 
                // en la variable 'ModelState' qué campo es el que sigue dando error.
                return View(newusuario);
            }
            // 1. Aquí generas un Token aleatorio (ej. un Guid)
            string miToken = Guid.NewGuid().ToString();

            string salt = HelperCryptography.GenerarSalt();

            string passwordConSalt = newusuario.Password + salt;

            string passwordHash = HelperCryptography.EncriptarTextoBasico(passwordConSalt);

            int randomAvatar = new Random().Next(1, 13);
            string avatarDefault = $"avatar{randomAvatar}.png";
            // 2. Guardas el usuario en Base de Datos (con el token y un campo 'Activo' = false)
            await this.repo.CreateUsuario(newusuario.Nombre, newusuario.Email, newusuario.Password, miToken, salt, passwordHash, avatarDefault);

            // 3. Envías el correo en 1 sola línea de código
            await mailService.EnviarEmailConfirmacionAsync(newusuario.Email, newusuario.Nombre, miToken);

            // 4. Te lo llevas a una vista que diga "Revisa tu email"
            return RedirectToAction("Login", "Managed");
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CambiarAvatar(string nombreAvatar)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario != null)
            {
                await this.repo.CambiarAvatarAsync(idUsuario, nombreAvatar);
            }
            return RedirectToAction("Perfil");
        }
        [HttpGet]
        public async Task<IActionResult> ActivarCuenta(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewData["ERROR"] = "Enlace de activación no válido.";
                return View("Login", "Managed");
            }

            // Llamamos al repositorio para que le ponga Activo = 1
            bool cuentaActivada = await this.repo.ActivarCuentaAsync(token);

            if (cuentaActivada)
            {
                ViewData["MENSAJE"] = "¡Cuenta activada con éxito! Ya puedes iniciar sesión y empezar a jugar.";
            }
            else
            {
                ViewData["ERROR"] = "El enlace ha caducado o la cuenta ya estaba activada.";
            }

            // Le devolvemos a la vista de Login con el mensaje
            return RedirectToAction("Login", "Managed");
        }
        [AuthorizeUsuario]
        [HttpGet]
        public async Task<IActionResult> Perfil(int? id, string buscar = null, string tab = null)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario == null) return RedirectToAction("Login", "Usuarios");

            int idPerfil = id ?? idUsuario;
            bool esMiPerfil = (idPerfil == idUsuario);

            // 1. CARGAMOS DATOS DEL PERFIL Y ESTADÍSTICAS
            InformacionUsuario usuario = await this.repo.PerfilUsuarioAsync(idPerfil);

            // Historial Solo
            List<HistorialIndividualPartidas> historialSolo = await this.repo.GetHistorialIndividualAsync(idPerfil);

            // NUEVO: Historial Multijugador
            List<HistorialMultiPartida> historialMulti = await this.repo.GetHistorialMultijugadorAsync(idPerfil);

            List<RankingModo> rankingModos = await this.repo.GetRankingsPorUsuarioAsync(idPerfil);
            ViewBag.RankingModos = rankingModos;

            ViewBag.TotalPartidas = historialSolo.Count; // Aquí podrías sumar historialMulti.Count si quieres el total absoluto
            ViewBag.TotalAciertos = historialSolo.Sum(h => h.PreguntasCorrectas);
            ViewBag.PorcentajeExito = historialSolo.Sum(h => h.PreguntasTotales) > 0
                ? Math.Round((double)historialSolo.Sum(h => h.PreguntasCorrectas) / historialSolo.Sum(h => h.PreguntasTotales) * 100, 1)
                : 0;

            // Pasamos solo los últimos 5 de cada tipo
            ViewBag.Historial = historialSolo.Take(5).ToList();
            ViewBag.HistorialMulti = historialMulti.Take(5).ToList();

            ViewBag.EsMiPerfil = esMiPerfil;
            ViewBag.TabActiva = "stats";

            ViewBag.TabActiva = tab ?? TempData["TabActiva"] ?? "stats";
            // 2. LÓGICA SOCIAL
            if (esMiPerfil)
            {
                ViewBag.Amigos = await this.repo.GetAmistadesAsync(idUsuario);
                ViewBag.Solicitudes = await this.repo.GetSolicitudesRecibidasAsync(idUsuario);

                if (!string.IsNullOrEmpty(buscar))
                {
                    ViewBag.ResultadosBusqueda = await this.repo.BuscarUsuariosNuevosAsync(idUsuario, buscar);
                    ViewBag.TabActiva = "social";
                }
            }

            return View(usuario);
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> DeleteUsuario(int idusuario)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await this.repo.DeleteUsuario(idusuario);
            return RedirectToAction("Login", "Managed");
        }
    }
}
