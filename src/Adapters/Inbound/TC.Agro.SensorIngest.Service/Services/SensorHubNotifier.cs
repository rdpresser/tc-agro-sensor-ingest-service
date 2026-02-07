using Microsoft.AspNetCore.SignalR;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Service.Hubs;

namespace TC.Agro.SensorIngest.Service.Services
{
    internal sealed class SensorHubNotifier : ISensorHubNotifier
    {
        private readonly IHubContext<SensorHub, ISensorHubClient> _hubContext;
        private readonly ILogger<SensorHubNotifier> _logger;

        public SensorHubNotifier(
            IHubContext<SensorHub, ISensorHubClient> hubContext,
            ILogger<SensorHubNotifier> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task NotifySensorReadingAsync(
            string sensorId,
            double? temperature,
            double? humidity,
            double? soilMoisture,
            DateTimeOffset timestamp,
            CancellationToken ct)
        {
            try
            {
                var dto = new SensorReadingHubDto(
                    sensorId,
                    string.Empty,
                    temperature,
                    humidity,
                    soilMoisture,
                    timestamp);

                await _hubContext.Clients.All.SensorReading(dto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast sensor reading for {SensorId}", sensorId);
            }
        }

        public async Task NotifyNewAlertAsync(
            Guid id,
            string severity,
            string title,
            string message,
            Guid plotId,
            string plotName,
            string sensorId,
            string status,
            DateTimeOffset createdAt,
            CancellationToken ct)
        {
            try
            {
                var dto = new AlertHubDto(id, severity, title, message, plotId, plotName, sensorId, status, createdAt);

                await _hubContext.Clients.All.NewAlert(dto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast new alert {AlertId}", id);
            }
        }

        public async Task NotifySensorStatusChangedAsync(
            string sensorId,
            string status,
            CancellationToken ct)
        {
            try
            {
                var dto = new SensorStatusChangedDto(sensorId, status);

                await _hubContext.Clients.All.SensorStatusChanged(dto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast status change for {SensorId}", sensorId);
            }
        }
    }
}
