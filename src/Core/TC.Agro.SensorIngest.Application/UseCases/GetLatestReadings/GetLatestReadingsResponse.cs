namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record GetLatestReadingsResponse(
        Guid Id,
        Guid SensorId,
        Guid PlotId,
        DateTimeOffset Time,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel);
}
