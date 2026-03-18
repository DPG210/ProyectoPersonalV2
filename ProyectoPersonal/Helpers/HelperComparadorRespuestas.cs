using Humanizer.Localisation;

namespace ProyectoPersonal.Helpers
{
    
public static class HelperComparadorRespuestas
    {
        private static int CalcularDistancia(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        public static bool EsRespuestaValida(string respuestaUsuario, string respuestaReal)
        {
            string userClean = respuestaUsuario.SimplifyForTrivia();
            string realClean = respuestaReal.SimplifyForTrivia();

            if (userClean == realClean) return true;

            int distancia = CalcularDistancia(userClean, realClean);

            if (realClean.Length < 4) return distancia == 0;
            if (realClean.Length <= 8) return distancia <= 1;
            return distancia <= 2;
        }
    }
}
