namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record LatestReadingItem(
        Guid Id,
        Guid SensorId,
        Guid PlotId,
        DateTime Time,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel);
}
