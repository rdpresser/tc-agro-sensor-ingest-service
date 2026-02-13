namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    public sealed record ReadingHistoryItem(
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
