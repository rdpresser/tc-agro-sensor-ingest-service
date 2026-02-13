namespace TC.Agro.SensorIngest.Service.Hubs
{
    public sealed record SensorReadingRequest(
        Guid SensorId,
        Guid PlotId,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        DateTimeOffset Timestamp);
}
