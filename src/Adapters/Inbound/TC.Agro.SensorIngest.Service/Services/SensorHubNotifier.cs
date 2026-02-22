namespace TC.Agro.SensorIngest.Service.Services
{
    internal sealed class SensorHubNotifier : ISensorHubNotifier
    {
        private readonly IHubContext<SensorHub, ISensorHubClient> _hubContext;
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly ILogger<SensorHubNotifier> _logger;

        public SensorHubNotifier(
            IHubContext<SensorHub, ISensorHubClient> hubContext,
            ISensorSnapshotStore snapshotStore,
            ILogger<SensorHubNotifier> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task NotifySensorReadingAsync(
            Guid sensorId,
            double? temperature,
            double? humidity,
            double? soilMoisture,
            DateTimeOffset timestamp)
        {
            try
            {
                var snapshot = await _snapshotStore.GetByIdAsync(sensorId).ConfigureAwait(false);

                if (snapshot is null)
                {
                    _logger.LogWarning("Cannot broadcast reading: SensorSnapshot not found for {SensorId}", sensorId);
                    return;
                }

                var dto = new SensorReadingRequest(
                    sensorId,
                    temperature,
                    humidity,
                    soilMoisture,
                    timestamp);

                await _hubContext.Clients.Group($"plot:{snapshot.PlotId}").SensorReading(dto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast sensor reading for {SensorId}", sensorId);
            }
        }

        public async Task NotifySensorStatusChangedAsync(
            Guid sensorId,
            string status)
        {
            try
            {
                var snapshot = await _snapshotStore.GetByIdAsync(sensorId).ConfigureAwait(false);

                if (snapshot is null)
                {
                    _logger.LogWarning("Cannot broadcast status change: SensorSnapshot not found for {SensorId}", sensorId);
                    return;
                }

                var dto = new SensorStatusChangedRequest(sensorId, status);

                await _hubContext.Clients.Group($"plot:{snapshot.PlotId}").SensorStatusChanged(dto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast status change for {SensorId}", sensorId);
            }
        }
    }
}
