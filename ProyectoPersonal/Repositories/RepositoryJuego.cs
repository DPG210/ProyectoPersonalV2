using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoPersonal.Data;
using ProyectoPersonal.Models;

namespace ProyectoPersonal.Repositories
{
    public class RepositoryJuego : IRepositoryJuego
    {
        private TrivialContext context;
        public RepositoryJuego(TrivialContext context)
        {
            this.context = context;
        }
        public async Task RegistrarRespuestaAsync(int partidaId, int usuarioId, int indicePregunta, bool esCorrecta, int puntos)
        {
            string sql = @"UPDATE PARTICIPANTES_PARTIDA 
                   SET puntuacion_actual = puntuacion_actual + @puntos,
                       indice_respondido = @indice
                   WHERE partida_id = @pid AND usuario_id = @uid";


            SqlParameter pamPun = new SqlParameter("@puntos", puntos);
            SqlParameter pamInd = new SqlParameter("@indice", indicePregunta);
            SqlParameter pamPartId = new SqlParameter("@pid", partidaId);
            SqlParameter pamIdU = new SqlParameter("@uid", usuarioId);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamPun, pamInd, pamPartId, pamIdU);
        }
        public async Task<bool> TodosHanRespondidoAsync(int partidaId, int indicePregunta)
        {
            var participantes = from datos in this.context.ParticipantesPartida
                                where datos.IdPartida == partidaId
                                select datos;

            bool existenParticipantes = await participantes.AnyAsync();
            bool todosListos = await participantes.AllAsync(p => p.IndiceRespondido >= indicePregunta);

            return existenParticipantes && todosListos;
        }

        public async Task<List<RankingJugador>> GetRankingAsync(int partidaId)
        {
            var consulta = from p in this.context.ParticipantesPartida
                           join u in this.context.Usuarios on p.IdUsuario equals u.IdUsuario
                           where p.IdPartida == partidaId
                           orderby p.PuntuacionActual descending 
                           select new RankingJugador
                           {
                               Nombre = u.Nombre,
                               Puntos = p.PuntuacionActual
                           };

            return await consulta.ToListAsync();
        }
        public async Task AvanzarJugadorAsync(int partidaId, int usuarioId, int nuevoIndice)
        {
            string sql = @"UPDATE PARTICIPANTES_PARTIDA 
                   SET indice_respondido = @indice
                   WHERE partida_id = @pid AND usuario_id = @uid";
            SqlParameter pamInd = new SqlParameter("@indice", nuevoIndice);
            SqlParameter pamPartId = new SqlParameter("@pid", partidaId);
            SqlParameter pamIdUsu = new SqlParameter("@uid", usuarioId);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamInd, pamPartId, pamIdUsu);
        }
        public async Task GuardarHistorialIndividualAsync(int idUsuario, string nombreCuestionario, int puntuacion, int correctas, int totales)
        {
            string sql = "sp_GuardarHistorialIndividual @usuario_id,@nombre_cuestionario,@puntuacion,@preguntas_correctas,@preguntas_totales";
            SqlParameter pamIdUsu = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamCue = new SqlParameter("@nombre_cuestionario", nombreCuestionario);
            SqlParameter pamPun = new SqlParameter("@puntuacion", puntuacion);
            SqlParameter pamPreC = new SqlParameter("@preguntas_correctas", correctas);
            SqlParameter pamPreT = new SqlParameter("@preguntas_totales", totales);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamIdUsu, pamCue, pamPun, pamPreC, pamPreT);
        }


        public async Task<List<HistorialIndividualPartidas>> GetHistorialIndividualAsync(int idUsuario)
        {
            var consulta = from h in this.context.HistorialIndividual
                           join c in this.context.Cuestionarios on h.IdCuestionario equals c.IdCuestionario
                           where h.IdUsuario == idUsuario
                           orderby h.FechaFinalizacion descending
                           select new HistorialIndividualPartidas
                           {
                               IdUsuario = h.IdUsuario,
                               IdCuestionario = h.IdCuestionario,
                               NombreCuestionario = c.Titulo,
                               Puntuacion = h.Puntuacion,
                               PreguntasCorrectas = h.PreguntasCorrectas,
                               PreguntasTotales = h.PreguntasTotales,
                               FechaFinalizacion = h.FechaFinalizacion
                           };

            return await consulta.ToListAsync();
        }
        public async Task<List<HistorialMultiPartida>> GetHistorialMultijugadorAsync(int idUsuario)
        {
            var consultaPartidas = from p in this.context.Partidas
                                   join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                                   join pp in this.context.ParticipantesPartida on p.IdPartida equals pp.IdPartida
                                   where pp.IdUsuario == idUsuario && p.Estado == "finalizada"
                                   orderby p.IdPartida descending
                                   select new HistorialMultiPartida
                                   {
                                       IdPartida = p.IdPartida,
                                       NombreCuestionario = c.Titulo
                                   };

            List<HistorialMultiPartida> historial = await consultaPartidas.ToListAsync();

            foreach (var partida in historial)
            {
                partida.Participantes = await (from pp in this.context.ParticipantesPartida
                                               join u in this.context.Usuarios on pp.IdUsuario equals u.IdUsuario
                                               where pp.IdPartida == partida.IdPartida
                                               orderby pp.PuntuacionActual descending
                                               select new ParticipantePartida
                                               {
                                                   IdUsuario = u.IdUsuario,
                                                   Nombre = u.Nombre,
                                                   Puntuacion = pp.PuntuacionActual,
                                                   HaRespondido = true
                                               }).ToListAsync();

                if (partida.Participantes.Any())
                {
                    partida.Ganador = partida.Participantes.First().Nombre;
                }
            }
            return historial;
        }

        public async Task GuardarRankingModoAsync(int idUsuario, string modoJuego, int puntos, string nombreCuestionario)
        {
            string sql = "sp_GuardarRankingModo @usuario_id, @modo_juego, @puntos,@nombre_cuestionario";

            SqlParameter pamId = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamModo = new SqlParameter("@modo_juego", modoJuego);
            SqlParameter pamPuntos = new SqlParameter("@puntos", puntos);
            SqlParameter pamCuestionario = new SqlParameter("@nombre_cuestionario", nombreCuestionario);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamModo, pamPuntos, pamCuestionario);
        }


        public async Task<List<RankingModo>> GetRankingsPorUsuarioAsync(int idUsuario)
        {
            var consulta = from r in this.context.RankingModos
                           join c in this.context.Cuestionarios
                           on r.IdCuestionario equals c.IdCuestionario 
                           where r.IdUsuario == idUsuario
                           orderby r.Puntos descending
                           select new RankingModo
                           {
                               IdUsuario = r.IdUsuario,
                               ModoJuego = r.ModoJuego,
                               IdCuestionario = r.IdCuestionario,
                               Puntos = r.Puntos,
                               FechaPartida = r.FechaPartida,
                               NombreCuestionario = c.Titulo 
                           };

            return await consulta.ToListAsync();
        }




        public async Task<List<dynamic>> GetTopRankingGlobalAsync(string modo, string filtro, string orden)
        {

            var consulta = from rank in this.context.RankingModos
                           join user in this.context.Usuarios on rank.IdUsuario equals user.IdUsuario
                           join quest in this.context.Cuestionarios on rank.IdCuestionario equals quest.IdCuestionario
                           join cat in this.context.Categorias on quest.IdCategoria equals cat.IdCategoria
                           where rank.ModoJuego.ToLower() == modo.ToLower()
                           select new
                           {
                               nombre = user.Nombre,
                               avatar = user.Avatar,
                               puntos = rank.Puntos,
                               cuestionario = quest.Titulo,
                               fecha = rank.FechaPartida,
                               nombreCategoria = cat.Nombre 
                           };

            if (!string.IsNullOrEmpty(filtro))
            {
                consulta = consulta.Where(r => r.nombreCategoria == filtro);
            }

            if (orden == "asc")
            {
                consulta = consulta.OrderBy(r => r.puntos);
            }
            else
            {
                consulta = consulta.OrderByDescending(r => r.puntos);
            }

            var resultados = await consulta.Take(10).ToListAsync();
            return resultados.Cast<dynamic>().ToList();
        }

        
    }
}
