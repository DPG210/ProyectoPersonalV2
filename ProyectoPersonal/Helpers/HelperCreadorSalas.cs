namespace ProyectoPersonal.Helpers
{
    public static class HelperCreadorSalas
    {
        public static string GenerateRoom()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            return new string(Enumerable.Repeat(chars, 6).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }
    }
}
