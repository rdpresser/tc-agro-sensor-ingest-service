using Microsoft.AspNetCore.Authorization;

namespace TC.Agro.SensorIngest.Service.Hubs
{
    [Authorize(Roles = "Admin,Producer")]
    public sealed class SensorHub : Hub<ISensorHubClient>
    {
        public async Task JoinPlotGroup(string plotId)
        {
            if (!Guid.TryParse(plotId, out var parsedPlotId) || parsedPlotId == Guid.Empty)
                throw new HubException("Invalid plotId. Must be a valid non-empty GUID.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"plot:{parsedPlotId}");
        }

        public async Task LeavePlotGroup(string plotId)
        {
            if (!Guid.TryParse(plotId, out var parsedPlotId) || parsedPlotId == Guid.Empty)
                throw new HubException("Invalid plotId. Must be a valid non-empty GUID.");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"plot:{parsedPlotId}");
        }
    }
}
