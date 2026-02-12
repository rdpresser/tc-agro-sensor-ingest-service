namespace TC.Agro.SensorIngest.Application.UseCases.GetSensorList
{
    public sealed record SensorListItem(
        Guid Id,
        string SensorId,
        Guid PlotId,
        string PlotName,
        string Status,
        double Battery,
        DateTimeOffset? LastReadingAt,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture);
}
