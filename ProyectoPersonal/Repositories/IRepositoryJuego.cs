using ProyectoPersonal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoPersonal.Repositories
{
    public interface IRepositoryJuego
    {
        // Acciones en tiempo de juego (Live Gameplay)
        Task RegistrarRespuestaAsync(int partidaId, int usuarioId, int indicePregunta, bool esCorrecta, int puntos);
        Task<bool> TodosHanRespondidoAsync(int partidaId, int indicePregunta);
        Task AvanzarJugadorAsync(int partidaId, int usuarioId, int nuevoIndice);
        Task<List<RankingJugador>> GetRankingAsync(int partidaId); // Ranking interno de la sala

        // Historiales Personales
        Task GuardarHistorialIndividualAsync(int idUsuario, string nombreCuestionario, int puntuacion, int correctas, int totales);
        Task<List<HistorialIndividualPartidas>> GetHistorialIndividualAsync(int idUsuario);
        Task<List<HistorialMultiPartida>> GetHistorialMultijugadorAsync(int idUsuario);

        // Rankings Globales
        Task GuardarRankingModoAsync(int idUsuario, string modoJuego, int puntos, string nombreCuestionario);
        Task<List<RankingModo>> GetRankingsPorUsuarioAsync(int idUsuario);

        // Nota: He cambiado 'dynamic' por la clase ViewModel que tipamos en pasos anteriores
        Task<List<VistaRankingGlobal>> GetTopRankingGlobalAsync(string modo, string filtro, string orden);
    }
}