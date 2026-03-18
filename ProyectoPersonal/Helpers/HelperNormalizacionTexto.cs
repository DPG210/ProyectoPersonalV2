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

        public static string SimplifyForTrivia(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            string result = text.RemoveAccents().ToLower().Trim();

            result = Regex.Replace(result, @"^(el|la|los|las|un|una|unos|unas)\s+", "");

            result = Regex.Replace(result, @"[^\w\s]", "");

            result = Regex.Replace(result, @"\s+", " ");

            return result;
        }
    }
}
