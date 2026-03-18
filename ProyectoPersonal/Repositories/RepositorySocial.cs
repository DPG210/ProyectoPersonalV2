using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoPersonal.Data;
using ProyectoPersonal.Models;

namespace ProyectoPersonal.Repositories
{
    public class RepositorySocial: IRepositorySocial
    {
        private TrivialContext context;
        public RepositorySocial(TrivialContext context)
        {
            this.context = context;
        }
        public async Task EnviarSolicitudAsync(int idEmisor, int idReceptor)
        {
            string sql = "sp_EnviarSolicitudAmistad @id_emisor,@id_receptor";
            SqlParameter pamEmi = new SqlParameter("@id_emisor", idEmisor);
            SqlParameter pamRec = new SqlParameter("@id_receptor", idReceptor);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamEmi, pamRec);
        }
        public async Task ResponderSolicitudAsync(int idEmisor, int idReceptor, string estado)
        {
            string sql = "sp_ResponderSolicitud @id_emisor, @id_receptor, @nuevo_estado";
            SqlParameter pamEmi = new SqlParameter("@id_emisor", idEmisor);
            SqlParameter pamRec = new SqlParameter("@id_receptor", idReceptor);
            SqlParameter pamEst = new SqlParameter("@nuevo_estado", estado);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamEmi, pamRec, pamEst);
        }

        public async Task<string> GetEstadoAmistadAsync(int idLogueado, int idPerfil)
        {
            var consulta = from datos in this.context.Amistades
                           where (datos.IdUsuario1 == idLogueado && datos.IdUsuario2 == idPerfil)
                              || (datos.IdUsuario1 == idPerfil && datos.IdUsuario2 == idLogueado)
                           select datos.Estado;

            // ExecuteScalarAsync se traduce aquí en FirstOrDefaultAsync
            // Si no hay fila, devolverá null automáticamente
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<List<string>> GetAmistadesAsync(int idUsuario)
        {
            var consulta = from amistad in this.context.Amistades
                           join usuario in this.context.Usuarios
                           on (amistad.IdUsuario1 == idUsuario ? amistad.IdUsuario2 : amistad.IdUsuario1)
                           equals usuario.IdUsuario
                           where (amistad.IdUsuario1 == idUsuario || amistad.IdUsuario2 == idUsuario)
                                 && amistad.Estado == "ACEPTADA"
                           select usuario.Nombre;

            return await consulta.ToListAsync();
        }
        public async Task<List<InformacionUsuario>> BuscarUsuariosNuevosAsync(int idLogueado, string buscar)
        {
            // 1. Obtenemos los IDs de usuarios con los que ya hay amistad (el "NOT IN" del SQL)
            var idsAmigos = await this.context.Amistades
                .Where(a => a.IdUsuario1 == idLogueado || a.IdUsuario2 == idLogueado)
                .Select(a => a.IdUsuario1 == idLogueado ? a.IdUsuario2 : a.IdUsuario1)
                .ToListAsync();

            // 2. Buscamos usuarios cuyo nombre coincida, que no seas tú y que no estén en la lista anterior
            var consulta = from datos in this.context.Usuarios
                           where datos.Nombre.Contains(buscar) // Esto es el LIKE '%buscar%'
                           && datos.IdUsuario != idLogueado
                           && !idsAmigos.Contains(datos.IdUsuario) // El NOT IN
                           select new InformacionUsuario
                           {
                               IdUsuario = datos.IdUsuario,
                               Nombre = datos.Nombre,
                           };

            return await consulta.ToListAsync();
        }
        public async Task<List<UsuarioAmistad>> GetSolicitudesRecibidasAsync(int idLogueado)
        {
            var consulta = from u in this.context.Usuarios
                           join a in this.context.Amistades
                           on u.IdUsuario equals a.IdUsuario1
                           where a.IdUsuario2 == idLogueado && a.Estado == "PENDIENTE"
                           select new UsuarioAmistad
                           {
                               IdUsuario = u.IdUsuario,
                               Nombre = u.Nombre
                           };

            return await consulta.ToListAsync();

        }
        public async Task RegistrarInvitacionAsync(int idEmisor, int idReceptor, string codigoSala)
        {
            // 1. Buscamos el ID de la partida usando LINQ (equivale al primer SELECT)
            var consultaId = from p in this.context.Partidas
                             where p.CodigoSala == codigoSala
                             select p.IdPartida;

            int idPartida = await consultaId.FirstOrDefaultAsync();

            // 2. Si la partida existe (id > 0), insertamos la invitación
            if (idPartida != 0)
            {
                string sql = @"INSERT INTO invitaciones 
                       (partida_id, usuario_emisor_id, usuario_receptor_id, estado, fecha_invitacion) 
                       VALUES (@idPartida, @idEmi, @idRec, 'pendiente', GETDATE())";

                SqlParameter pamPartida = new SqlParameter("@idPartida", idPartida);
                SqlParameter pamEmi = new SqlParameter("@idEmi", idEmisor);
                SqlParameter pamRec = new SqlParameter("@idRec", idReceptor);

                await this.context.Database.ExecuteSqlRawAsync(sql, pamPartida, pamEmi, pamRec);
            }
        }
        public async Task<List<InvitacionPartida>> GetInvitacionesPendientesAsync(int idReceptor)
        {
            var consulta = from i in this.context.Invitaciones
                           join u in this.context.Usuarios on i.IdEmisor equals u.IdUsuario
                           join p in this.context.Partidas on i.IdPartida equals p.IdPartida
                           join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                           where i.IdReceptor == idReceptor && i.Estado == "pendiente"
                           select new InvitacionPartida
                           {
                               IdInvitacion = i.IdInvitacion,
                               NombreEmisor = u.Nombre,
                               CodigoSala = p.CodigoSala,
                               TituloCuestionario = c.Titulo
                           };

            return await consulta.ToListAsync();
        }
        public async Task ActualizarEstadoInvitacionAsync(int idReceptor, string codigoSala, string nuevoEstado)
        {
            // Buscamos la invitación que coincida con el usuario y la partida (vía su código)
            string sql = @"UPDATE invitaciones 
                   SET estado = @estado 
                   WHERE usuario_receptor_id = @idRec 
                   AND partida_id = (SELECT partida_id FROM partidas WHERE codigo_sala = @cod)";


            SqlParameter pamEst = new SqlParameter("@estado", nuevoEstado);
            SqlParameter pamRec = new SqlParameter("@idRec", idReceptor);
            SqlParameter pamCod = new SqlParameter("@cod", codigoSala);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamEst, pamRec, pamCod);
        }
        public async Task<int> GetNumeroSolicitudesPendientesAsync(int idLogueado)
        {

            return await this.context.Amistades
                .CountAsync(a => a.IdUsuario2 == idLogueado && a.Estado == "PENDIENTE");
        }
        public async Task<List<Usuario>> BuscarUsuariosPorNombreAsync(string nombreBusqueda, int idLogueado)
        {

            return await this.context.Usuarios
                .Where(u => u.Nombre.Contains(nombreBusqueda) && u.IdUsuario != idLogueado)
                .Take(10)
                .ToListAsync();
        }
    }
}
