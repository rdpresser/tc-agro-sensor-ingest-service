namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    public sealed record GetReadingsHistoryResponse(
        Guid Id,
        Guid SensorId,
        Guid PlotId,
        string PlotName,
        string PropertyName,
        DateTimeOffset Time,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel);
}
