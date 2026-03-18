using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoPersonal.Data;
using ProyectoPersonal.Models;

namespace ProyectoPersonal.Repositories
{
    public class RepositoryCuestionarios:IRepositoryCuestionarios
    {
        private TrivialContext context;
        public RepositoryCuestionarios(TrivialContext context)
        {
            this.context = context;
        }
        public async Task<List<string>> GetCategoriasAsync()
        {
            var consulta = (from datos in this.context.Categorias
                            select datos.Nombre).Distinct();

            return await consulta.ToListAsync();

        }
        public async Task<int> GetIdCuestionarioByNombreAsync(string nombre)
        {
            var consulta = from datos in this.context.Cuestionarios
                           where datos.Titulo == nombre
                           select datos.IdCuestionario;
            return await consulta.FirstOrDefaultAsync();
        }
        public async Task<List<string>> GetCuestionariosAsync(string nombreCategoria, int idUsuario)
        {
            var consultaCategoria = from datos in this.context.Categorias
                                    where datos.Nombre == nombreCategoria
                                    select datos.IdCategoria;
            int idCategoria = await consultaCategoria.FirstOrDefaultAsync();


            var consultaTitulo = from datos in this.context.Cuestionarios
                                 where datos.IdCategoria == idCategoria &&
                                (datos.EsPublico == true ||
                                 datos.CreadorId == idUsuario)
                                 select datos.Titulo;
            return await consultaTitulo.ToListAsync();
        }
        public async Task<string> FindCuestionarioAsync(int idCuestionario)
        {
            var consulta = from datos in this.context.Cuestionarios
                           where datos.IdCuestionario == idCuestionario
                           select datos.Titulo;

            return await consulta.FirstOrDefaultAsync();
        }
        public async Task<List<string>> GetAllNombresCuestionariosPublicosAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Cuestionarios
                           where datos.EsPublico == true
                           || datos.CreadorId == idUsuario
                           select datos.Titulo;

            return await consulta.ToListAsync();
        }
        public async Task<List<Cuestionario>> GetCuestionariosUsuarioAsync(int idUsuario)
        {
            return await this.context.Cuestionarios
                .Where(z => z.CreadorId == idUsuario)
                .ToListAsync();
        }
        public async Task<List<Cuestionario>> GetCuestionarioCompletoAsync(string nombreCategoria, int idUsuario)
        {
            var consultaCategoria = from datos in this.context.Categorias
                                    where datos.Nombre == nombreCategoria
                                    select datos.IdCategoria;

            int idCategoria = await consultaCategoria.FirstOrDefaultAsync();
            var consultaCuestionario = from datos in this.context.Cuestionarios
                                       where datos.IdCategoria == idCategoria &&
                                      (datos.EsPublico == true ||
                                       datos.CreadorId == idUsuario)
                                       select datos;

            return await consultaCuestionario.ToListAsync();
        }
        public async Task CreateCuestionarioAsync(string categoria, string titulo, string descripcion, int idUsuario, bool esPublico)
        {
            string sql = "sp_CrearCuestionarioSimple @nombre_categoria,@titulo,@descripcion,@creador_id,@es_publico";
            SqlParameter pamCat = new SqlParameter("@nombre_categoria", categoria);
            SqlParameter pamTit = new SqlParameter("@titulo", titulo);
            SqlParameter pamDes = new SqlParameter("@descripcion", descripcion);
            SqlParameter pamCre = new SqlParameter("@creador_id", idUsuario);
            SqlParameter pamPub = new SqlParameter("@es_publico", esPublico);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamCat, pamTit, pamDes, pamCre, pamPub);
        }

        public async Task InsertPreguntasAsync
            (int idCuestionario, string enunciado, string opc_correct, string opc_incorrect_1, string opc_incorrect_2, string opc_incorrect_3, int nivel, string? explicacion)
        {

            string sql = "sp_InsertarPreguntaQuiz @cuestionario_id,@enunciado,@opcion_correcta," +
                "@opcion_incorrecta1,@opcion_incorrecta2,@opcion_incorrecta3,@nivel,@explicacion_didactica";
            SqlParameter pamCue = new SqlParameter("@cuestionario_id", idCuestionario);
            SqlParameter pamEnu = new SqlParameter("@enunciado", enunciado);
            SqlParameter pamOpc = new SqlParameter("@opcion_correcta", opc_correct);
            SqlParameter pamOpi1 = new SqlParameter("@opcion_incorrecta1", opc_incorrect_1);
            SqlParameter pamOpi2 = new SqlParameter("@opcion_incorrecta2", opc_incorrect_2);
            SqlParameter pamOpi3 = new SqlParameter("@opcion_incorrecta3", opc_incorrect_3);
            SqlParameter pamNiv = new SqlParameter("@nivel", nivel);
            SqlParameter pamExp = new SqlParameter("@explicacion_didactica", explicacion);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamCue, pamEnu, pamOpc, pamOpi1, pamOpi2, pamOpi3, pamNiv, pamExp);
        }
        public async Task<string> GetRespuestaCorrectaAsync(int idPregunta)
        {
            var consulta = from datos in this.context.Preguntas
                           where datos.IdPregunta == idPregunta
                           select datos.OpcionCorrecta;
            return await consulta.FirstOrDefaultAsync();
        }
        public async Task EnviarReporteAsync(int idPregunta, int idUsuario, string motivo, string comentario)
        {
            string sql = @"
                INSERT INTO REPORTES_PREGUNTAS 
                (pregunta_id, usuario_id, motivo_reporte, comentario_adicional, fecha_reporte, estado_reporte)
                VALUES 
                (@idPregunta, @idUsuario, @motivo, @comentario, GETDATE(), 'ABIERTO')";

            SqlParameter pamPregunta = new SqlParameter("@idPregunta", idPregunta);
            SqlParameter pamUsuario = new SqlParameter("@idUsuario", idUsuario);
            SqlParameter pamMotivo = new SqlParameter("@motivo", string.IsNullOrEmpty(motivo) ? (object)DBNull.Value : motivo);
            SqlParameter pamComentario = new SqlParameter("@comentario", string.IsNullOrEmpty(comentario) ? (object)DBNull.Value : comentario);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamPregunta, pamUsuario, pamMotivo, pamComentario);
        }
        public async Task<List<VistaReportePregunta>> GetReportesAbiertosAsync()
        {
            var consulta = from r in this.context.ReportePreguntas
                           join p in this.context.Preguntas
                           on r.IdPregunta equals p.IdPregunta
                           join u in this.context.Usuarios
                           on r.IdUsuario equals u.IdUsuario
                           where r.EstadoReporte == "ABIERTO"
                           orderby r.FechaReporte ascending
                           select new VistaReportePregunta
                           {
                               IdReporte = r.IdReporte,
                               IdPregunta = r.IdPregunta,
                               EnunciadoPregunta = p.Enunciado,
                               NombreUsuario = u.Nombre,
                               Motivo = r.MotivoReporte,
                               Comentario = r.ComentarioAdicional,
                               Fecha = r.FechaReporte,
                               Estado = r.EstadoReporte
                           };

            return await consulta.ToListAsync();
        }
        public async Task ResolverReporteAsync(int idReporte)
        {
            string sql = "UPDATE REPORTES_PREGUNTAS SET estado_reporte = 'CERRADO' WHERE reporte_id = @id";
            SqlParameter pamId = new SqlParameter("@id", idReporte);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamId);
        }

        public async Task<List<int>> GetIdsPreguntasAsync(string nombreCuestionario, int? nivel)
        {

            if (string.IsNullOrEmpty(nombreCuestionario) || nombreCuestionario == "MIXTO")
            {
                var consultaMixta = from p in this.context.Preguntas
                                    join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                                    where c.CreadorId == 1
                                    select p.IdPregunta;
                return await consultaMixta.ToListAsync();
            }


            var consulta = from p in this.context.Preguntas
                           join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                           where c.Titulo.Trim().ToLower() == nombreCuestionario.Trim().ToLower()
                           select p;


            if (nivel.HasValue && nivel.Value > 0)
            {
                consulta = consulta.Where(p => p.Nivel == nivel.Value);
            }


            return await consulta.Select(p => p.IdPregunta).ToListAsync();
        }


        public async Task<Pregunta> GetPreguntaByIdAsync(int idPregunta)
        {
            var consulta = from datos in this.context.Preguntas
                           where datos.IdPregunta == idPregunta
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }
    }
}
