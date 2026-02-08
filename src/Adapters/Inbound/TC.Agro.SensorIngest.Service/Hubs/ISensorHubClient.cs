namespace TC.Agro.SensorIngest.Service.Hubs
{
    public interface ISensorHubClient
    {
        Task SensorReading(SensorReadingHubDto reading);
        Task NewAlert(AlertHubDto alert);
        Task SensorStatusChanged(SensorStatusChangedDto data);
    }

    public sealed record SensorReadingHubDto(
        string SensorId,
        Guid PlotId,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        DateTimeOffset Timestamp);

    public sealed record AlertHubDto(
        Guid Id,
        string Severity,
        string Title,
        string Message,
        Guid PlotId,
        string PlotName,
        string SensorId,
        string Status,
        DateTimeOffset CreatedAt);

    public sealed record SensorStatusChangedDto(
        string SensorId,
        string Status);
}
