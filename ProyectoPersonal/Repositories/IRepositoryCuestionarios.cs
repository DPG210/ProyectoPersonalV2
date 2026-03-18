using ProyectoPersonal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoPersonal.Repositories
{
    public interface IRepositoryCuestionarios
    {
        Task<List<string>> GetCategoriasAsync();
        Task<int> GetIdCuestionarioByNombreAsync(string nombre);
        Task<List<string>> GetCuestionariosAsync(string nombreCategoria, int idUsuario);
        Task<string> FindCuestionarioAsync(int idCuestionario);
        Task<List<string>> GetAllNombresCuestionariosPublicosAsync(int idUsuario);
        Task<List<Cuestionario>> GetCuestionariosUsuarioAsync(int idUsuario);
        Task<List<Cuestionario>> GetCuestionarioCompletoAsync(string nombreCategoria, int idUsuario);
        Task CreateCuestionarioAsync(string categoria, string titulo, string descripcion, int idUsuario, bool esPublico);
        Task InsertPreguntasAsync(int idCuestionario, string enunciado, string opc_correct, string opc_incorrect_1, string opc_incorrect_2, string opc_incorrect_3, int nivel, string? explicacion);
        Task<string> GetRespuestaCorrectaAsync(int idPregunta);
        Task<List<int>> GetIdsPreguntasAsync(string nombreCuestionario, int? nivel);
        Task<Pregunta> GetPreguntaByIdAsync(int idPregunta);
        Task EnviarReporteAsync(int idPregunta, int idUsuario, string motivo, string comentario);
        Task<List<VistaReportePregunta>> GetReportesAbiertosAsync();
        Task ResolverReporteAsync(int idReporte);
    }
}