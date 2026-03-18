using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoPersonal.Data;
using ProyectoPersonal.Helpers;
using ProyectoPersonal.Models;

namespace ProyectoPersonal.Repositories
{
    public class RepositoryUsuarios:IRepositoryUsuarios
    {
        private TrivialContext context;
        public RepositoryUsuarios(TrivialContext context)
        {
            this.context = context;
        }
        public async Task CreateUsuario(string nombre, string email, string password, string token, string salt, string pass_hash, string avatar)
        {
            string sql = "SP_CREAR_USUARIO @nombre_usuario,@email,@password,@password_hash,@salt,@token,@avatar";
            SqlParameter pamNom = new SqlParameter("@nombre_usuario", nombre);
            SqlParameter pamEm = new SqlParameter("@email", email);
            SqlParameter pamPass = new SqlParameter("@password", password);
            SqlParameter pamTok = new SqlParameter("@token", token);
            SqlParameter pamSalt = new SqlParameter("@salt", salt);
            SqlParameter pamPassH = new SqlParameter("@password_hash", pass_hash);
            SqlParameter pamAvatar = new SqlParameter("@avatar", avatar);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamNom, pamEm, pamPass, pamPassH, pamSalt, pamTok, pamAvatar);
        }
        public async Task<List<Usuario>> GetUsuariosASync()
        {
            var consulta = from datos in this.context.Usuarios
                           select datos;
            return await consulta.ToListAsync();
        }
        public async Task<Usuario> LoginUsuarioAsync(string username, string password)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.Email == username || datos.Nombre == username
                           select datos;
            Usuario usuario = await consulta.FirstOrDefaultAsync();

            if (usuario == null) return null;

            var consultaSeguridad = from datos in this.context.Logins
                                    where datos.IdUsuario == usuario.IdUsuario
                                    select datos;
            Login seguridad = await consultaSeguridad.FirstOrDefaultAsync();

            if (seguridad == null) return null;

            string passwordConSalt = password + seguridad.Salt;
            string hashCalculado = HelperCryptography.EncriptarTextoBasico(passwordConSalt);

            if (hashCalculado == seguridad.PasswordHash)
            {
                return usuario;
            }

            return null;
        }
        public async Task<bool> ActivarCuentaAsync(string token)
        {
            var usuario = await this.context.Usuarios
                                    .FirstOrDefaultAsync(u => u.TokenMail == token);

            if (usuario != null)
            {
                usuario.Activo = true; 
                usuario.TokenMail = null; 

                await this.context.SaveChangesAsync(); 
                return true;
            }

            return false;
        }

        public async Task CambiarAvatarAsync(int idUsuario, string nuevoAvatar)
        {
            string sql = "UPDATE USUARIOS SET avatar = @avatar WHERE usuario_id = @id";
            SqlParameter pamAv = new SqlParameter("@avatar", nuevoAvatar);
            SqlParameter pamId = new SqlParameter("@id", idUsuario);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamAv, pamId);
        }
        public async Task<InformacionUsuario> PerfilUsuarioAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos;
            Usuario user = await consulta.FirstOrDefaultAsync();
            InformacionUsuario usuario = new InformacionUsuario();
            usuario.IdUsuario = user.IdUsuario;
            usuario.Nombre = user.Nombre;
            usuario.Email = user.Email;
            usuario.Corazones = user.CorazonesActuales;
            usuario.Avatar = user.Avatar;
            return usuario;
        }
        public async Task<string> FindNombreUsuarioAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos.Nombre;
            return await consulta.FirstOrDefaultAsync();
        }
        public async Task<int> GetIdUsuarioByNombreAsync(string nombre)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.Nombre == nombre
                           select datos.IdUsuario;
            return await consulta.FirstOrDefaultAsync();
        }
        public async Task<int> ActualizarYObtenerCorazonesAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos;
            Usuario usuario = await consulta.FirstOrDefaultAsync();
            if (usuario == null) return 0;

            if (usuario.CorazonesActuales >= usuario.CorazonesMaximos)
            {
                return usuario.CorazonesActuales;
            }

            if (usuario.UltimaRecarga == null)
            {
                usuario.UltimaRecarga = DateTime.Now;
                await this.context.SaveChangesAsync();
                return usuario.CorazonesActuales;
            }

            int minutosParaRecarga = 30;
            DateTime ahora = DateTime.Now;

            var tiempoTranscurrido = ahora - usuario.UltimaRecarga.Value;
            int corazonesGanados = (int)tiempoTranscurrido.TotalMinutes / minutosParaRecarga;

            if (corazonesGanados > 0)
            {
                int nuevosCorazones = usuario.CorazonesActuales + corazonesGanados;

                if (nuevosCorazones >= usuario.CorazonesMaximos)
                {
                    usuario.CorazonesActuales = usuario.CorazonesMaximos;
                    usuario.UltimaRecarga = ahora; 
                }
                else
                {
                    usuario.CorazonesActuales = nuevosCorazones;
                    usuario.UltimaRecarga = usuario.UltimaRecarga.Value.AddMinutes(corazonesGanados * minutosParaRecarga);
                }

                await this.context.SaveChangesAsync();
            }

            return usuario.CorazonesActuales;
        }
        public async Task<bool> RecargarPorAnuncioAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos;
            Usuario usuario = await consulta.FirstOrDefaultAsync();
            if (usuario == null) return false;

            DateTime hoy = DateTime.Now;

            if (usuario.FechaUltimoAnuncio == null || usuario.FechaUltimoAnuncio.Value.Date != hoy.Date)
            {
                usuario.AnunciosVistosHoy = 0;
            }

            if (usuario.AnunciosVistosHoy >= 3)
            {
                return false;
            }

            if (usuario.CorazonesActuales < usuario.CorazonesMaximos)
            {
                usuario.CorazonesActuales++;
                usuario.AnunciosVistosHoy++;
                usuario.FechaUltimoAnuncio = hoy;

                await this.context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        public async Task<bool> ConsumirCorazonAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos;
            Usuario usuario = await consulta.FirstOrDefaultAsync();
            if (usuario == null || usuario.CorazonesActuales <= 0)
            {
                return false;
            }

            if (usuario.CorazonesActuales == usuario.CorazonesMaximos)
            {
                usuario.UltimaRecarga = DateTime.Now;
            }

            usuario.CorazonesActuales--;
            await this.context.SaveChangesAsync();

            return true;
        }
        public async Task DeleteUsuario(int idUsuario)
        {
            string sql = " sp_EliminarUsuario @usuario_id";
            SqlParameter pamIdUsuario = new SqlParameter("@usuario_id", idUsuario);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamIdUsuario);
        }
        public async Task RegistrarPagoYActivarVipAsync(int idUsuario, string sessionId, string tipoPlan)
        {
            decimal monto = (tipoPlan == "anual") ? 9.99m : 0.99m;


            Pago pago = new Pago
            {
                IdUsuario = idUsuario,
                StripeSessionId = sessionId,
                Monto = monto,
                PlanTipo = tipoPlan,
                FechaPago = DateTime.Now
            };

            this.context.Pagos.Add(pago);

            var user = await this.context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (user != null)
            {
                user.RolId = 2; 
                user.CorazonesActuales = 999; 
                user.CorazonesMaximos = 999;
            }

            await this.context.SaveChangesAsync();
        }
        public async Task<Usuario> GenerarTokenRecuperacionAsync(string email)
        {

            Usuario usuario = await this.context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);


            if (usuario == null) return null;


            usuario.TokenMail = Guid.NewGuid().ToString();

            await this.context.SaveChangesAsync();

            return usuario;
        }
        public async Task<bool> ResetPasswordAsync(string token, string passwordNormal, string passwordHash, string salt)
        {
            string sql = "sp_CambiarPassword @usuario_id, @nuevo_password, @nuevo_hash, @nuevo_salt";

            Usuario usuario = await this.context.Usuarios.FirstOrDefaultAsync(u => u.TokenMail == token);

            if (usuario == null) return false;

            SqlParameter pamId = new SqlParameter("@usuario_id", usuario.IdUsuario);
            SqlParameter pamPass = new SqlParameter("@nuevo_password", passwordNormal);
            SqlParameter pamHash = new SqlParameter("@nuevo_hash", passwordHash);
            SqlParameter pamSalt = new SqlParameter("@nuevo_salt", salt);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamPass, pamHash, pamSalt);

            return true;
        }
        public async Task<string> ComprobarUsuarioDuplicadoAsync(string nombre, string email)
        {
            bool existeEmail = await this.context.Usuarios.AnyAsync(u => u.Email == email);
            if (existeEmail) return "email";

            bool existeNombre = await this.context.Usuarios.AnyAsync(u => u.Nombre == nombre);
            if (existeNombre) return "nombre";

            return null;
        }

        public async Task CambiarPasswordDesdePerfilAsync(int idUsuario, string passwordNormal, string passwordHash, string salt)
        {
            string sql = "sp_CambiarPassword @usuario_id, @nuevo_password, @nuevo_hash, @nuevo_salt";

            SqlParameter pamId = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamPass = new SqlParameter("@nuevo_password", passwordNormal);
            SqlParameter pamHash = new SqlParameter("@nuevo_hash", passwordHash);
            SqlParameter pamSalt = new SqlParameter("@nuevo_salt", salt);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamPass, pamHash, pamSalt);
        }
    }
}
