using ProyectoPersonal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoPersonal.Repositories
{
    public interface IRepositoryJuego
    {
        Task RegistrarRespuestaAsync(int partidaId, int usuarioId, int indicePregunta, bool esCorrecta, int puntos);
        Task<bool> TodosHanRespondidoAsync(int partidaId, int indicePregunta);
        Task AvanzarJugadorAsync(int partidaId, int usuarioId, int nuevoIndice);
        Task<List<RankingJugador>> GetRankingAsync(int partidaId); 
        Task GuardarHistorialIndividualAsync(int idUsuario, string nombreCuestionario, int puntuacion, int correctas, int totales);
        Task<List<HistorialIndividualPartidas>> GetHistorialIndividualAsync(int idUsuario);
        Task<List<HistorialMultiPartida>> GetHistorialMultijugadorAsync(int idUsuario);
        Task GuardarRankingModoAsync(int idUsuario, string modoJuego, int puntos, string nombreCuestionario);
        Task<List<RankingModo>> GetRankingsPorUsuarioAsync(int idUsuario);
        Task<List<dynamic>> GetTopRankingGlobalAsync(string modo, string filtro, string orden);
    }
}