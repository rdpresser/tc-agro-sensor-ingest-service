namespace TC.Agro.SensorIngest.Service.Hubs
{
    public sealed record SensorReadingRequest(
        Guid SensorId,
        Guid PlotId,
        string? SensorLabel,
        string PlotName,
        string PropertyName,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        DateTimeOffset Timestamp);
}
