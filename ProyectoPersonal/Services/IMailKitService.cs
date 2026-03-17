namespace ProyectoPersonal.Services
{
    public interface IMailKitService
    {
        
        Task EnviarEmailConfirmacionAsync(string emailDestino, string nombreUsuario, string token);

        
        Task EnviarEmailRecuperacionAsync(string emailDestino, string nombreUsuario, string token);
    }
}
