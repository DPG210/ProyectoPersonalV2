using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProyectoPersonal.Controllers
{
    public class BaseController : Controller
    {
            protected int UsuarioActualId =>
                int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            protected string UsuarioActualNombre =>
                User.FindFirst(ClaimTypes.Name)!.Value;

            protected string UsuarioActualAvatar =>
                User.FindFirst("Avatar")?.Value ?? "avatar1.png";
            protected string UsuarioActualRole =>
                User.FindFirst("Role")?.Value;

            protected bool EsAdmin =>
                User.IsInRole("ADMIN");
        
    }
}
