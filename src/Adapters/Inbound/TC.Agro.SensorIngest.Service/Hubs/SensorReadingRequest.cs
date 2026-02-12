namespace TC.Agro.SensorIngest.Service.Hubs
{
    public sealed record SensorReadingRequest(
        string SensorId,
        Guid PlotId,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        DateTimeOffset Timestamp);
}
