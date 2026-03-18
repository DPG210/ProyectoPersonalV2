using ProyectoPersonal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoPersonal.Repositories
{
    public interface IRepositorySalas
    {
        Task<SalaJuego> CreateSalaJuegoAsync(int idAnfitrion, int idCuestionario, string tipoJuego, int cantidad, int tiempo, bool publica, int capacidad);
        Task<ParticipantePartida> UnirseAPartidaAsync(int idUsuario, string codigoSala);
        Task<List<ParticipantePartida>> GetParticipantesSalaAsync(string codigoSala);
        Task<SalaJuego> GetSalaPorCodigoAsync(string codigoPartida);
        Task<List<SalaJuego>> GetSalasPublicasAsync();

        // Control de Estado de las Salas
        Task CambiarEstadoPartidaAsync(int idSala, string nuevoEstado);
        Task<bool> CancelarSalaAnfitrionAsync(int idSala, int idAnfitrion);
        Task FinalizarPartidaMultijugadorAsync(int idPartida);
    }
}