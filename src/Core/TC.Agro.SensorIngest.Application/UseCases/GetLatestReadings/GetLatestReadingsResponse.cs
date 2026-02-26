namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record GetLatestReadingsResponse(
        Guid Id,
        Guid PlotId,
        Guid SensorId,
        string? SensorLabel,
        string PlotName,
        string PropertyName,
        DateTimeOffset Time,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel);
}
