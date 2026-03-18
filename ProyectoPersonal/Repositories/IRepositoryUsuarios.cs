using ProyectoPersonal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoPersonal.Repositories
{
    public interface IRepositoryUsuarios
    {
        Task CreateUsuario(string nombre, string email, string password, string token, string salt, string pass_hash, string avatar);
        Task<List<Usuario>> GetUsuariosASync();
        Task<Usuario> LoginUsuarioAsync(string username, string password);
        Task<bool> ActivarCuentaAsync(string token);
        Task CambiarAvatarAsync(int idUsuario, string nuevoAvatar);
        Task<InformacionUsuario> PerfilUsuarioAsync(int idUsuario);
        Task<string> FindNombreUsuarioAsync(int idUsuario);
        Task<int> GetIdUsuarioByNombreAsync(string nombre);
        Task DeleteUsuario(int idUsuario);

        // Seguridad y Recuperación
        Task<Usuario> GenerarTokenRecuperacionAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string passwordNormal, string passwordHash, string salt);
        Task CambiarPasswordDesdePerfilAsync(int idUsuario, string passwordNormal, string passwordHash, string salt);
        Task<string> ComprobarUsuarioDuplicadoAsync(string nombre, string email);

        // Economía y VIP
        Task<int> ActualizarYObtenerCorazonesAsync(int idUsuario);
        Task<bool> RecargarPorAnuncioAsync(int idUsuario);
        Task<bool> ConsumirCorazonAsync(int idUsuario);
        Task RegistrarPagoYActivarVipAsync(int idUsuario, string sessionId, string tipoPlan);
    }
}