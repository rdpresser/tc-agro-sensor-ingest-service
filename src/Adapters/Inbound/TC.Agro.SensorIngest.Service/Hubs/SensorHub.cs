using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TC.Agro.SensorIngest.Service.Hubs
{
    [Authorize(Roles = "Admin,Producer")]
    public sealed class SensorHub : Hub<ISensorHubClient>
    {
        public async Task JoinPlotGroup(string plotId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"plot:{plotId}");
        }

        public async Task LeavePlotGroup(string plotId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"plot:{plotId}");
        }
    }
}
