

using Microsoft.AspNetCore.SignalR;

namespace ProyectoPersonal.Hubs
{
    public class TrivialHub:Hub
    {
        public async Task UnirseAPartida(string partidaId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, partidaId);
        }
    }
}
