namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorHubNotifier
    {
        Task NotifySensorReadingAsync(
            string sensorId,
            double? temperature,
            double? humidity,
            double? soilMoisture,
            DateTimeOffset timestamp,
            CancellationToken ct = default);

        Task NotifyNewAlertAsync(
            Guid id,
            string severity,
            string title,
            string message,
            Guid plotId,
            string plotName,
            string sensorId,
            string status,
            DateTimeOffset createdAt,
            CancellationToken ct = default);

        Task NotifySensorStatusChangedAsync(
            string sensorId,
            string status,
            CancellationToken ct = default);
    }
}
