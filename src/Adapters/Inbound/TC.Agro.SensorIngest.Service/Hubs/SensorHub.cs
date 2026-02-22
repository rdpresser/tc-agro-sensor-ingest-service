using Microsoft.AspNetCore.Authorization;

namespace TC.Agro.SensorIngest.Service.Hubs
{
    [Authorize(Roles = "Admin,Producer")]
    public sealed class SensorHub : Hub<ISensorHubClient>
    {
        private readonly ISensorReadingRepository _readingRepository;

        public SensorHub(ISensorReadingRepository readingRepository)
        {
            _readingRepository = readingRepository;
        }

        public async Task JoinPlotGroup(string plotId)
        {
            if (!Guid.TryParse(plotId, out var parsedPlotId) || parsedPlotId == Guid.Empty)
                throw new HubException("Invalid plotId. Must be a valid non-empty GUID.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"plot:{parsedPlotId}");

            var recentReadings = await _readingRepository
                .GetByPlotIdAsync(parsedPlotId, limit: 10)
                .ConfigureAwait(false);

            foreach (var reading in recentReadings)
            {
                await Clients.Caller.SensorReading(new SensorReadingRequest(
                    reading.SensorId,
                    reading.Temperature,
                    reading.Humidity,
                    reading.SoilMoisture,
                    reading.Time)).ConfigureAwait(false);
            }
        }

        public async Task LeavePlotGroup(string plotId)
        {
            if (!Guid.TryParse(plotId, out var parsedPlotId) || parsedPlotId == Guid.Empty)
                throw new HubException("Invalid plotId. Must be a valid non-empty GUID.");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"plot:{parsedPlotId}");
        }
    }
}
