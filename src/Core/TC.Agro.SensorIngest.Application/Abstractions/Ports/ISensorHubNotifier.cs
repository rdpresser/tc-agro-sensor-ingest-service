namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorHubNotifier
    {
        Task NotifySensorReadingAsync(
            Guid sensorId,
            double? temperature,
            double? humidity,
            double? soilMoisture,
            DateTimeOffset timestamp);

        Task NotifySensorStatusChangedAsync(
            Guid sensorId,
            string status);
    }
}
