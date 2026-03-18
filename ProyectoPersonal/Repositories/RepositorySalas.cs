using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoPersonal.Data;
using ProyectoPersonal.Models;
using System.Data;
using System.Data.Common;

namespace ProyectoPersonal.Repositories
{
    public class RepositorySalas: IRepositorySalas
    {
        private TrivialContext context;
        public RepositorySalas(TrivialContext context)
        {
            this.context = context;
        }
        public async Task<SalaJuego> CreateSalaJuegoAsync(int idAnfitrion, int idCuestionario, string tipoJuego, int cantidad, int tiempo, bool publica, int capacidad)
        {
            string codigoSala = Helpers.HelperCreadorSalas.GenerateRoom();

            var consulta = from datos in this.context.Cuestionarios
                           where datos.IdCuestionario == idCuestionario
                           select datos.Titulo;

            string titulo = await consulta.FirstOrDefaultAsync();

            Usuario usuarioAnfitrion = await this.context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idAnfitrion);
            int idPartida;
            using (DbCommand com = this.context.Database.GetDbConnection().CreateCommand())
            {
                com.CommandType = CommandType.StoredProcedure;
                com.CommandText = "sp_CrearPartida";

                com.Parameters.Add(new SqlParameter("@anfitrion_id", idAnfitrion));
                com.Parameters.Add(new SqlParameter("@cuestionario_id", idCuestionario));
                com.Parameters.Add(new SqlParameter("@codigo_sala", codigoSala));
                com.Parameters.Add(new SqlParameter("@tipo_juego", tipoJuego));
                com.Parameters.Add(new SqlParameter("@cantidad_preguntas", cantidad));
                com.Parameters.Add(new SqlParameter("@tiempo_pregunta", tiempo));
                com.Parameters.Add(new SqlParameter("@es_publica", publica));
                com.Parameters.Add(new SqlParameter("@capacidad_maxima", capacidad));

                await com.Connection.OpenAsync();
                idPartida = Convert.ToInt32(await com.ExecuteScalarAsync());
                await com.Connection.CloseAsync();
            }

            SalaJuego sala = new SalaJuego
            {
                IdSala = idPartida,
                CodigoSala = codigoSala,
                IdAnfitrion = idAnfitrion,
                NombreAnfitrion = usuarioAnfitrion.Nombre,
                AvatarAnfitrion = usuarioAnfitrion.Avatar,
                NombreCuestionario = titulo,
                Estado = "esperando",
                TipoJuego = tipoJuego,
                CantidadPreguntas = cantidad,
                Tiempo = tiempo,
                Publica = publica,
                CapacidadMaxima = capacidad,
                TotalJugadores = 1,
                Participantes = new List<ParticipantePartida>
                {
                    new ParticipantePartida
                    {
                        IdUsuario = idAnfitrion,
                        Nombre = usuarioAnfitrion.Nombre,
                        Puntuacion = 0,
                        HaRespondido = false
                    }
                }
            };

            return sala;
        }
        public async Task<ParticipantePartida> UnirseAPartidaAsync(int idUsuario, string codigoSala)
        {
            SalaJuego sala = await this.GetSalaPorCodigoAsync(codigoSala);

            if (sala == null || sala.Estado != "esperando" || sala.TotalJugadores >= sala.CapacidadMaxima)
            {
                return null; 
            }
            
            string sql = "sp_UnirseAPartida @usuario_id, @codigo_sala";
            SqlParameter pamId = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamCod = new SqlParameter("@codigo_sala", codigoSala);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamCod);
            if (sala.TotalJugadores + 1 == sala.CapacidadMaxima)
            {
                await this.CambiarEstadoPartidaAsync(sala.IdSala, "jugando");
            }
            string nombre = await this.context.Usuarios
                           .Where(u => u.IdUsuario == idUsuario)
                           .Select(u => u.Nombre)
                           .FirstOrDefaultAsync(); 


            ParticipantePartida participante = new ParticipantePartida
            {
                IdUsuario = idUsuario,
                Nombre = nombre,
                Puntuacion = 0,
                HaRespondido = false
            };

            return participante;
        }
        public async Task<List<ParticipantePartida>> GetParticipantesSalaAsync(string codigoSala)
        {
            var consulta = from p in this.context.ParticipantesPartida
                           join pr in this.context.Partidas on p.IdPartida equals pr.IdPartida
                           join u in this.context.Usuarios on p.IdUsuario equals u.IdUsuario
                           where pr.CodigoSala == codigoSala
                           select new ParticipantePartida
                           {
                               IdUsuario = u.IdUsuario,
                               Nombre = u.Nombre,
                               Puntuacion = p.PuntuacionActual,
                               HaRespondido = false
                           };

            return await consulta.ToListAsync();
        }

        public async Task<SalaJuego> GetSalaPorCodigoAsync(string codigoPartida)
        {
            var consulta = from p in this.context.Partidas
                           join u in this.context.Usuarios on p.IdAnfitrion equals u.IdUsuario
                           join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                           where p.CodigoSala == codigoPartida
                           select new SalaJuego
                           {
                               IdSala = p.IdPartida,
                               CodigoSala = p.CodigoSala,
                               Estado = p.Estado,
                               IdAnfitrion = p.IdAnfitrion,
                               NombreAnfitrion = u.Nombre,
                               NombreCuestionario = c.Titulo,
                               TipoJuego = p.TipoJuego,
                               CantidadPreguntas = p.CantidadPreguntas,
                               Publica = p.EsPublica,
                               Tiempo = p.TiempoPregunta,
                               CapacidadMaxima = p.CapacidadMaxima,
                               Participantes = new List<ParticipantePartida>() 
                           };

            SalaJuego sala = await consulta.FirstOrDefaultAsync();

            if (sala != null)
            {
                sala.Participantes = await this.GetParticipantesSalaAsync(sala.CodigoSala);
                sala.TotalJugadores = sala.Participantes.Count;
            }

            return sala;
        }
        public async Task CambiarEstadoPartidaAsync(int idSala, string nuevoEstado)
        {

            string sql = "UPDATE partidas SET estado = @estado WHERE partida_id = @id";

            SqlParameter pamEst = new SqlParameter("@estado", nuevoEstado);
            SqlParameter pamId = new SqlParameter("@id", idSala);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamEst, pamId);
        }
        public async Task FinalizarPartidaMultijugadorAsync(int idPartida)
        {
            string sql = "sp_FinalizarPartidaMulti @partida_id";
            SqlParameter pamParFin = new SqlParameter("partida_id", idPartida);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamParFin);
        }
        public async Task<List<SalaJuego>> GetSalasPublicasAsync()
        {
            var consulta = from datos in this.context.SalasPublicas
                           select new SalaJuego
                           {
                               IdSala = datos.IdPartida,
                               CodigoSala = datos.CodigoSala,
                               IdAnfitrion = datos.IdAnfitrion,
                               NombreAnfitrion = datos.NombreAnfitrion,
                               AvatarAnfitrion = datos.AvatarAnfrition,
                               NombreCuestionario = datos.NombreCuestionario,
                               TipoJuego = datos.TipoJuego,
                               CantidadPreguntas = datos.CantidadPreguntas,
                               Publica = datos.EsPublica,
                               TotalJugadores = datos.TotalJugadores,
                               Tiempo = datos.TiempoPregunta,
                               Participantes = new List<ParticipantePartida>()
                           };

            return await consulta.ToListAsync();
        }
        public async Task<bool> CancelarSalaAnfitrionAsync(int idSala, int idAnfitrion)
        {

            string sql = @"UPDATE partidas 
                   SET estado = 'finalizada' 
                   WHERE partida_id = @idSala 
                   AND anfitrion_id = @idAnfitrion 
                   AND estado = 'esperando'";

            Microsoft.Data.SqlClient.SqlParameter pamSala = new Microsoft.Data.SqlClient.SqlParameter("@idSala", idSala);
            Microsoft.Data.SqlClient.SqlParameter pamAnf = new Microsoft.Data.SqlClient.SqlParameter("@idAnfitrion", idAnfitrion);


            int filasAfectadas = await this.context.Database.ExecuteSqlRawAsync(sql, pamSala, pamAnf);


            return filasAfectadas > 0;
        }
        public async Task<bool> CerrarSalaAdminAsync(int idSala)
        {
            string sql = @"UPDATE partidas 
                   SET estado = 'finalizada' 
                   WHERE partida_id = @idSala";

            Microsoft.Data.SqlClient.SqlParameter pamSala = new Microsoft.Data.SqlClient.SqlParameter("@idSala", idSala);

            int filasAfectadas = await this.context.Database.ExecuteSqlRawAsync(sql, pamSala);

            return filasAfectadas > 0;
        }
    }
}
