using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

public class MailKitService
{
    private readonly IConfiguration _config;

    public MailKitService(IConfiguration config)
    {
        _config = config;
    }

    public async Task EnviarEmailConfirmacionAsync(string emailDestino, string nombreUsuario, string tokenConfirmacion)
    {
        // 1. LEER LA CONFIGURACIÓN DE TU APPSETTINGS
        string user = _config.GetValue<string>("MailSettings:Credentials:User");
        string pass = _config.GetValue<string>("MailSettings:Credentials:Password");
        string host = _config.GetValue<string>("MailSettings:Server:Host");
        int port = _config.GetValue<int>("MailSettings:Server:Port");
        bool useSsl = _config.GetValue<bool>("MailSettings:Server:Ssl");

        // 2. FABRICAR EL MENSAJE
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress("Trivial Challenge", user));
        email.To.Add(new MailboxAddress(nombreUsuario, emailDestino));
        email.Subject = "Confirma tu cuenta en Trivial Challenge 🎮";

        // URL del token (cambiaremos el localhost por tu puerto real de Visual Studio)
        string urlConfirmacion = $"https://localhost:7113/Trivial/ActivarCuenta?token={tokenConfirmacion}";

        email.Body = new TextPart(TextFormat.Html)
        {
            Text = $@"
                <div style='font-family: Arial; padding: 20px; background-color: #f8fafc; border-radius: 10px;'>
                    <h2 style='color: #0d9488;'>¡Bienvenido, {nombreUsuario}!</h2>
                    <p>Ya casi estás listo para jugar. Haz clic en el siguiente enlace para activar tu cuenta:</p>
                    <br>
                    <a href='{urlConfirmacion}' style='background-color: #0d9488; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>Activar mi cuenta</a>
                </div>"
        };

        // 3. ENVIAR EL MENSAJE CON MAILKIT
        using var smtp = new SmtpClient();
        try
        {
            // Nos conectamos usando la info de tu JSON
            SecureSocketOptions opcionesSeguridad = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await smtp.ConnectAsync(host, port, opcionesSeguridad);

            // Nos autenticamos con tu usuario y contraseña
            await smtp.AuthenticateAsync(user, pass);

            // Enviamos el correo
            await smtp.SendAsync(email);
        }
        catch (Exception ex)
        {
            // Aquí puedes poner un Console.WriteLine para ver si Outlook te bloquea el intento
            Console.WriteLine($"Error al enviar correo: {ex.Message}");
            throw;
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}