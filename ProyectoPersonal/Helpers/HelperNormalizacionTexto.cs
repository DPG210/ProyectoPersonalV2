using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProyectoPersonal.Helpers
{
    public static class HelperNormalizacionTexto
    {

        public static string RemoveAccents(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // El "Limpiador Maestro" que usa el anterior
        public static string SimplifyForTrivia(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            // 1. Quitar acentos, pasar a minúsculas y quitar espacios extremos
            string result = text.RemoveAccents().ToLower().Trim();

            // 2. Quitar artículos iniciales (opcional pero recomendado para Trivial)
            // Esto quita "el ", "la ", "los ", "las ", "un ", "una " al principio
            result = Regex.Replace(result, @"^(el|la|los|las|un|una|unos|unas)\s+", "");

            // 3. Quitar signos de puntuación
            result = Regex.Replace(result, @"[^\w\s]", "");

            // 4. Limpiar espacios dobles internos
            result = Regex.Replace(result, @"\s+", " ");

            return result;
        }
    }
}
