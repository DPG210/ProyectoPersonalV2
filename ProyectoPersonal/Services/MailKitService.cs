using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ProyectoPersonal.Services
{
    public class MailKitService : IMailKitService
    {
        private readonly IConfiguration _config;

        public MailKitService(IConfiguration config)
        {
            _config = config;
        }

        
        public async Task EnviarEmailRecuperacionAsync(string emailDestino, string nombreUsuario, string token)
        {
            string urlRecuperacion = $"https://localhost:7113/Managed/ResetPassword?token={token}";

            string mensajeHtml = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #fff7ed; border-radius: 10px; border: 1px solid #ffedd5;'>
                <h2 style='color: #ea580c;'>¿Has olvidado tu contraseña?</h2>
                <p>Hola <strong>{nombreUsuario}</strong>,</p>
                <p>Hemos recibido una solicitud para restablecer tu clave de acceso al Trivial Challenge.</p>
                <p>Si fuiste tú, pulsa el botón de abajo para elegir una nueva contraseña:</p>
                <br>
                <a href='{urlRecuperacion}' style='background-color: #ea580c; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>Restablecer Contraseña</a>
                <p style='font-size: 11px; color: #9a3412; margin-top: 20px;'>Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
            </div>";

            await EnviarEmailBaseAsync(emailDestino, nombreUsuario, "Recupera tu contraseña - Trivial Challenge 🔑", mensajeHtml);
        }

        public async Task EnviarEmailConfirmacionAsync(string emailDestino, string nombreUsuario, string token)
        {
            string urlConfirmacion = $"https://localhost:7113/Managed/ActivarCuenta?token={token}";

            string mensajeHtml = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #f8fafc; border-radius: 10px;'>
                <h2 style='color: #0d9488;'>¡Bienvenido, {nombreUsuario}!</h2>
                <p>Gracias por unirte al desafío. Para activar tu cuenta de comandante, haz clic en el botón:</p>
                <br>
                <a href='{urlConfirmacion}' style='background-color: #0d9488; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>Activar mi cuenta</a>
            </div>";

            await EnviarEmailBaseAsync(emailDestino, nombreUsuario, "Confirma tu cuenta en Trivial Challenge 🎮", mensajeHtml);
        }

        private async Task EnviarEmailBaseAsync(string destino, string nombre, string asunto, string cuerpoHtml)
        {
            string user = _config.GetValue<string>("MailSettings:Credentials:User");
            string pass = _config.GetValue<string>("MailSettings:Credentials:Password");
            string host = _config.GetValue<string>("MailSettings:Server:Host");
            int port = _config.GetValue<int>("MailSettings:Server:Port");
            bool useSsl = _config.GetValue<bool>("MailSettings:Server:Ssl");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Trivial Challenge", user));
            email.To.Add(new MailboxAddress(nombre, destino));
            email.Subject = asunto;
            email.Body = new TextPart(TextFormat.Html) { Text = cuerpoHtml };

            using var smtp = new SmtpClient();

            SecureSocketOptions options = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            await smtp.ConnectAsync(host, port, options);
            await smtp.AuthenticateAsync(user, pass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}