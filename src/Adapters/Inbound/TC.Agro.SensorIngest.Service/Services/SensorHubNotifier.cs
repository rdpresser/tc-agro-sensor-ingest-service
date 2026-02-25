using TC.Agro.SharedKernel.Infrastructure.Caching.Service;

namespace TC.Agro.SensorIngest.Service.Services
{
    internal sealed class SensorHubNotifier : ISensorHubNotifier
    {
        private static readonly TimeSpan PlotIdCacheDuration = TimeSpan.FromMinutes(10);

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
            string? label,
            double? temperature,
            double? humidity,
            double? soilMoisture,
            DateTimeOffset timestamp)
        {
            try
            {
                var plotId = await ResolvePlotIdAsync(sensorId).ConfigureAwait(false);

                if (plotId is null)
                {
                    _logger.LogWarning("Cannot broadcast reading: SensorSnapshot not found for {SensorId}", sensorId);
                    return;
                }

                var dto = new SensorReadingRequest(
                    sensorId,
                    label,
                    temperature,
                    humidity,
                    soilMoisture,
                    timestamp);

                await _hubContext.Clients.Group($"plot:{plotId}").SensorReading(dto).ConfigureAwait(false);
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
                var plotId = await ResolvePlotIdAsync(sensorId).ConfigureAwait(false);

                if (plotId is null)
                {
                    _logger.LogWarning("Cannot broadcast status change: SensorSnapshot not found for {SensorId}", sensorId);
                    return;
                }

                var dto = new SensorStatusChangedRequest(sensorId, status);

                await _hubContext.Clients.Group($"plot:{plotId}").SensorStatusChanged(dto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast status change for {SensorId}", sensorId);
            }
        }

        private async Task<Guid?> ResolvePlotIdAsync(Guid sensorId)
        {
            var cacheKey = $"sensor:plotId:{sensorId}";

            return await _cacheService.GetOrSetAsync<Guid?>(
                cacheKey,
                async ct =>
                {
                    var snapshot = await _snapshotStore.GetByIdAsync(sensorId, ct).ConfigureAwait(false);
                    return snapshot?.PlotId;
                },
                PlotIdCacheDuration).ConfigureAwait(false);
        }
    }
}
