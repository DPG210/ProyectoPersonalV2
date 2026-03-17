using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ProyectoPersonal.Helpers
{
    public class HelperCryptography
    {
        public static string GenerarSalt()
        {
            byte[] saltBytes = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }
        public static string EncriptarTextoBasico(string contenido)
        {
            byte[] entrada;
            byte[] salida;
            UnicodeEncoding encoding= new UnicodeEncoding();
            SHA256 managed= SHA256.Create();
            entrada= encoding.GetBytes(contenido);
            salida= managed.ComputeHash(entrada);
            string resultado= Convert.ToBase64String(salida);
            return resultado;
        }
    }
}
