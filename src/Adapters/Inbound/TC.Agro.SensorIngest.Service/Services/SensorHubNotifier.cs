using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SharedKernel.Infrastructure.Caching.Service;

namespace TC.Agro.SensorIngest.Service.Services
{
    internal sealed class SensorHubNotifier : ISensorHubNotifier
    {
        private static readonly TimeSpan _plotIdCacheDuration = TimeSpan.FromMinutes(10);

        private readonly IHubContext<SensorHub, ISensorHubClient> _hubContext;
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SensorHubNotifier> _logger;

        public SensorHubNotifier(
            IHubContext<SensorHub, ISensorHubClient> hubContext,
            ISensorSnapshotStore snapshotStore,
            ICacheService cacheService,
            ILogger<SensorHubNotifier> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
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
                var sensor = await ResolvePlotAsync(sensorId).ConfigureAwait(false);
                if (sensor is null)
                {
                    _logger.LogWarning("Cannot broadcast reading: SensorSnapshot not found for {SensorId}", sensorId);
                    return;
                }

                var notification = new SensorReadingRequest(
                    sensorId,
                    sensor.PlotId,
                    sensor.Label,
                    sensor.PlotName,
                    sensor.PropertyName,
                    temperature,
                    humidity,
                    soilMoisture,
                    timestamp);

                await _hubContext.Clients
                    .Groups(new[] { $"plot:{sensor.PlotId}", $"owner:{sensor.OwnerId}" })
                    .SensorReading(notification)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "Sensor reading broadcast to plot {PlotId} (SensorId: {SensorId}, Temp: {Temperature}, Humidity: {Humidity}, SoilMoisture: {SoilMoisture})",
                    sensor.PlotId,
                    sensorId,
                    temperature,
                    humidity,
                    soilMoisture);
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
                var sensor = await ResolvePlotAsync(sensorId).ConfigureAwait(false);
                if (sensor is null)
                {
                    _logger.LogWarning("Cannot broadcast status change: SensorSnapshot not found for {SensorId}", sensorId);
                    return;
                }

                var notification = new SensorStatusChangedRequest(sensorId, status);

                await _hubContext.Clients
                    .Groups(new[] { $"plot:{sensor.PlotId}", $"owner:{sensor.OwnerId}" })
                    .SensorStatusChanged(notification)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast status change for {SensorId}", sensorId);
            }
        }

        private async Task<SensorSnapshot?> ResolvePlotAsync(Guid sensorId)
        {
            string cacheKey = $"sensor:plotId:{sensorId}";

            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async ct =>
                {
                    var snapshot = await _snapshotStore.GetByIdAsync(sensorId, ct).ConfigureAwait(false);
                    return snapshot;
                },
                _plotIdCacheDuration).ConfigureAwait(false);
        }
    }
}
