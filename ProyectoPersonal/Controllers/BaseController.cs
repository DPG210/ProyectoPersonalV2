using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class BaseController : Controller
    {
            protected int UsuarioActualId =>
                int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new InvalidOperationException("Usuario no autenticado."));

            protected string UsuarioActualNombre =>
                User.FindFirst(ClaimTypes.Name)?.Value
                    ?? throw new InvalidOperationException("Usuario no autenticado.");

            protected string UsuarioActualAvatar =>
                User.FindFirst("Avatar")?.Value ?? "avatar1.png";
            protected string UsuarioActualRole =>
                User.FindFirst(ClaimTypes.Role)?.Value
                    ?? throw new InvalidOperationException("Usuario no autenticado.");

            protected bool EsAdmin =>
                User.IsInRole("ADMIN");
        
    }
}
