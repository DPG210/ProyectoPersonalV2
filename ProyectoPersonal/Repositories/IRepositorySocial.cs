using ProyectoPersonal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoPersonal.Repositories
{
    public interface IRepositorySocial
    {
        Task EnviarSolicitudAsync(int idEmisor, int idReceptor);
        Task ResponderSolicitudAsync(int idEmisor, int idReceptor, string estado);
        Task<string> GetEstadoAmistadAsync(int idLogueado, int idPerfil);
        Task<List<string>> GetAmistadesAsync(int idUsuario);
        Task<List<InformacionUsuario>> BuscarUsuariosNuevosAsync(int idLogueado, string buscar);
        Task<List<UsuarioAmistad>> GetSolicitudesRecibidasAsync(int idLogueado);
        Task<int> GetNumeroSolicitudesPendientesAsync(int idLogueado);
        Task<List<Usuario>> BuscarUsuariosPorNombreAsync(string nombreBusqueda, int idLogueado);
        Task RegistrarInvitacionAsync(int idEmisor, int idReceptor, string codigoSala);
        Task<List<InvitacionPartida>> GetInvitacionesPendientesAsync(int idReceptor);
        Task ActualizarEstadoInvitacionAsync(int idReceptor, string codigoSala, string nuevoEstado);
    }
}