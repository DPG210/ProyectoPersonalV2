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
            // 1. Buscamos al usuario que tenga exactamente ese token
            var usuario = await this.context.Usuarios
                                    .FirstOrDefaultAsync(u => u.TokenMail == token);

            // 2. Si lo encontramos, le cambiamos los datos
            if (usuario != null)
            {
                usuario.Activo = true; // ¡Cuenta activada!
                usuario.TokenMail = null; // Borramos el token por seguridad para que no se re-use

                await this.context.SaveChangesAsync(); // Guardamos los cambios en SQL
                return true;
            }

            // Si no lo encuentra (token inventado o ya usado)
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

            // Si ya está al máximo, devolvemos el valor y no hacemos cálculos
            if (usuario.CorazonesActuales >= usuario.CorazonesMaximos)
            {
                return usuario.CorazonesActuales;
            }

            // Si por algún motivo la última recarga es nula, la iniciamos ahora
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
                    usuario.UltimaRecarga = ahora; // Reseteamos el cronómetro
                }
                else
                {
                    usuario.CorazonesActuales = nuevosCorazones;
                    // Sumamos solo los bloques de 30 min para no robarle los minutos sobrantes
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

            // 1. Reseteamos el contador si es un día nuevo
            if (usuario.FechaUltimoAnuncio == null || usuario.FechaUltimoAnuncio.Value.Date != hoy.Date)
            {
                usuario.AnunciosVistosHoy = 0;
            }

            // 2. Comprobamos el límite diario (ejemplo: 3 anuncios máximo)
            if (usuario.AnunciosVistosHoy >= 3)
            {
                return false;
            }

            // 3. Otorgamos el corazón si no está al máximo
            if (usuario.CorazonesActuales < usuario.CorazonesMaximos)
            {
                usuario.CorazonesActuales++;
                usuario.AnunciosVistosHoy++;
                usuario.FechaUltimoAnuncio = hoy;

                // Nota: NO tocamos usuario.UltimaRecarga. 
                // El cronómetro pasivo sigue corriendo por detrás.

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

            // Si los corazones estaban al máximo, el cronómetro de recarga debe empezar JUSTO AHORA
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
            // 1. Definir precio según el plan
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

            // 3. Buscar usuario y darle poderes VIP
            var user = await this.context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (user != null)
            {
                user.RolId = 2; // Rol PREMIUM
                user.CorazonesActuales = 999; // Corazones infinitos
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
            // Comprobamos primero el correo (suele ser lo más crítico)
            bool existeEmail = await this.context.Usuarios.AnyAsync(u => u.Email == email);
            if (existeEmail) return "email";

            // Luego comprobamos el nombre
            bool existeNombre = await this.context.Usuarios.AnyAsync(u => u.Nombre == nombre);
            if (existeNombre) return "nombre";

            // Si llega aquí, es que todo está libre
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
