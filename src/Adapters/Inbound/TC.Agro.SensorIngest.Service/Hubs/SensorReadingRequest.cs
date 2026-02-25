namespace TC.Agro.SensorIngest.Service.Hubs
{
    public sealed record SensorReadingRequest(
        Guid SensorId,
        string? Label,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        DateTimeOffset Timestamp);
}
