using MailKit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProyectoPersonal.Filter;
using ProyectoPersonal.Helpers;
using ProyectoPersonal.Models;
using ProyectoPersonal.Repositories;
using ProyectoPersonal.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProyectoPersonal.Controllers
{
    public class ManagedController : Controller
    {
        private IRepositoryUsuarios repoUsuarios;
        private IRepositoryJuego repoJuego;
        private IRepositorySocial repoSocial;
        private IMailKitService mailService;

        public ManagedController(
            IRepositoryUsuarios repoUsuarios,
            IRepositoryJuego repoJuego,
            IRepositorySocial repoSocial,
            IMailKitService mailService)
        {
            this.repoUsuarios = repoUsuarios;
            this.repoJuego = repoJuego;
            this.repoSocial = repoSocial;
            this.mailService = mailService;
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            Usuario usuario = await this.repoUsuarios.LoginUsuarioAsync(username, password);
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
            return RedirectToAction("Index", "Partidas");


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

            if (!ModelState.IsValid)
            {
                return View(newusuario);
            }

            string duplicado = await this.repoUsuarios.ComprobarUsuarioDuplicadoAsync(newusuario.Nombre, newusuario.Email);

            if (duplicado == "email")
            {
                ViewData["ERROR"] = "Ya existe un jugador registrado con ese correo electrónico.";
                return View(newusuario); 
            }
            else if (duplicado == "nombre")
            {
                ViewData["ERROR"] = "Ese nombre ya está en uso. ¡Elige otro!";
                return View(newusuario);
            }
            
            string miToken = Guid.NewGuid().ToString();

            string salt = HelperCryptography.GenerarSalt();

            string passwordConSalt = newusuario.Password + salt;

            string passwordHash = HelperCryptography.EncriptarTextoBasico(passwordConSalt);

            int randomAvatar = new Random().Next(1, 13);
            string avatarDefault = $"avatar{randomAvatar}.png";
            
            await this.repoUsuarios.CreateUsuario(newusuario.Nombre, newusuario.Email, newusuario.Password, miToken, salt, passwordHash, avatarDefault);

            await mailService.EnviarEmailConfirmacionAsync(newusuario.Email, newusuario.Nombre, miToken);

            return RedirectToAction("Login", "Managed");
        }
        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CambiarAvatar(string nombreAvatar)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (idUsuario != null)
            {
                await this.repoUsuarios.CambiarAvatarAsync(idUsuario, nombreAvatar);
            }
            return RedirectToAction("Perfil", "Managed");
        }
        [HttpGet]
        public async Task<IActionResult> ActivarCuenta(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewData["ERROR"] = "Enlace de activación no válido.";
                return View("Login", "Managed");
            }

            bool cuentaActivada = await this.repoUsuarios.ActivarCuentaAsync(token);

            if (cuentaActivada)
            {
                ViewData["MENSAJE"] = "¡Cuenta activada con éxito! Ya puedes iniciar sesión y empezar a jugar.";
            }
            else
            {
                ViewData["ERROR"] = "El enlace ha caducado o la cuenta ya estaba activada.";
            }

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

            
            InformacionUsuario usuario = await this.repoUsuarios.PerfilUsuarioAsync(idPerfil);

            List<HistorialIndividualPartidas> historialSolo = await this.repoJuego.GetHistorialIndividualAsync(idPerfil);
            
            List<HistorialMultiPartida> historialMulti = await this.repoJuego.GetHistorialMultijugadorAsync(idPerfil);

            List<RankingModo> rankingModos = await this.repoJuego.GetRankingsPorUsuarioAsync(idPerfil);
            ViewBag.RankingModos = rankingModos;

            ViewBag.TotalPartidas = historialSolo.Count; 
            ViewBag.TotalAciertos = historialSolo.Sum(h => h.PreguntasCorrectas);
            ViewBag.PorcentajeExito = historialSolo.Sum(h => h.PreguntasTotales) > 0
                ? Math.Round((double)historialSolo.Sum(h => h.PreguntasCorrectas) / historialSolo.Sum(h => h.PreguntasTotales) * 100, 1)
                : 0;

            
            ViewBag.Historial = historialSolo.Take(5).ToList();
            ViewBag.HistorialMulti = historialMulti.Take(5).ToList();

            ViewBag.EsMiPerfil = esMiPerfil;
            ViewBag.TabActiva = "stats";

            ViewBag.TabActiva = tab ?? TempData["TabActiva"] ?? "stats";
            
            if (esMiPerfil)
            {
                ViewBag.Amigos = await this.repoSocial.GetAmistadesAsync(idUsuario);
                ViewBag.Solicitudes = await this.repoSocial.GetSolicitudesRecibidasAsync(idUsuario);

                if (!string.IsNullOrEmpty(buscar))
                {
                    ViewBag.ResultadosBusqueda = await this.repoSocial.BuscarUsuariosNuevosAsync(idUsuario, buscar);
                    ViewBag.TabActiva = "social";
                }
            }

            return View(usuario);
        }
        [AuthorizeUsuario]
        public async Task<IActionResult> DeleteUsuario(int idusuario)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await this.repoUsuarios.DeleteUsuario(idusuario);
            return RedirectToAction("Login", "Managed");
        }
        public IActionResult OlvidoPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OlvidoPassword(string email)
        {
            Usuario usuario = await this.repoUsuarios.GenerarTokenRecuperacionAsync(email);

            if (usuario != null)
            {
                await this.mailService.EnviarEmailRecuperacionAsync(usuario.Email, usuario.Nombre, usuario.TokenMail);

                ViewBag.Mensaje = "Si el correo existe en nuestra base, recibirás un enlace de recuperación pronto.";
            }
            else
            {
                ViewBag.Error = "No hemos encontrado ninguna cuenta asociada a ese correo.";
            }

            return View();
        }
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ERROR"] = "El enlace de recuperación no es válido o está incompleto.";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string password)
        {
            string salt = HelperCryptography.GenerarSalt();
            string passwordConSalt = password + salt;
            string passwordHash = HelperCryptography.EncriptarTextoBasico(passwordConSalt);

            bool actualizado = await this.repoUsuarios.ResetPasswordAsync(token, password, passwordHash, salt);

            if (actualizado)
            {
                TempData["MENSAJE"] = "¡Contraseña actualizada! Ya puedes entrar con tu nueva clave.";
                return RedirectToAction("Login");
            }
            else
            {
                ViewBag.Error = "El enlace ha caducado o es inválido. Inténtalo de nuevo.";
                return View();
            }
        }
        [AuthorizeUsuario]
        [HttpGet]
        public IActionResult CambiarPassword()
        {
            return View();
        }

        [AuthorizeUsuario]
        [HttpPost]
        public async Task<IActionResult> CambiarPassword(string oldPassword, string newPassword)
        {
            string username = User.Identity.Name;
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            Usuario usuarioValido = await this.repoUsuarios.LoginUsuarioAsync(username, oldPassword);

            if (usuarioValido == null)
            {
                ViewData["ERROR"] = "La contraseña actual no es correcta. Operación cancelada.";
                return View();
            }

            string salt = HelperCryptography.GenerarSalt();
            string passwordConSalt = newPassword + salt;
            string passwordHash = HelperCryptography.EncriptarTextoBasico(passwordConSalt);

            await this.repoUsuarios.CambiarPasswordDesdePerfilAsync(idUsuario, newPassword, passwordHash, salt);

            ViewData["MENSAJE"] = "¡Tu contraseña ha sido actualizado con éxito!";
            return View(); ;
        }
    }
}
