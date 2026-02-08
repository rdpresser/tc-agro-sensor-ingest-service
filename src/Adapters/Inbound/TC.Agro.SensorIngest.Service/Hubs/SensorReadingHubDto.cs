namespace TC.Agro.SensorIngest.Service.Hubs
{
    public sealed record SensorReadingHubDto(
        string SensorId,
        Guid PlotId,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        DateTimeOffset Timestamp);
}
