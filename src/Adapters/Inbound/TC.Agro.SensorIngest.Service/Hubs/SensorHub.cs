using Microsoft.AspNetCore.Authorization;

namespace TC.Agro.SensorIngest.Service.Hubs
{
    [Authorize(Roles = "Admin,Producer")]
    public sealed class SensorHub : Hub<ISensorHubClient>
    {
        private readonly ISensorReadingRepository _readingRepository;
        private readonly ISensorSnapshotStore _snapshotStore;

        public SensorHub(ISensorReadingRepository readingRepository, ISensorSnapshotStore snapshotStore)
        {
            _readingRepository = readingRepository;
            _snapshotStore = snapshotStore;
        }

        public async Task JoinPlotGroup(string plotId)
        {
            if (!Guid.TryParse(plotId, out var parsedPlotId) || parsedPlotId == Guid.Empty)
                throw new HubException("Invalid plotId. Must be a valid non-empty GUID.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"plot:{parsedPlotId}");

            var sensor = await _snapshotStore.GetByPlotIdAsync(parsedPlotId, Context.ConnectionAborted);
            if (sensor is null)
            {
                return;
            }

            var recentReadings = await _readingRepository
                .GetByPlotIdAsync(parsedPlotId, from: null, to: null, limit: 20, cancellationToken: Context.ConnectionAborted)
                .ConfigureAwait(false);

            foreach (var reading in recentReadings)
            {
                await Clients.Caller.SensorReading(new SensorReadingRequest(
                    reading.SensorId,
                    sensor.PlotId,
                    sensor.Label,
                    sensor.PlotName,
                    sensor.PropertyName,
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
