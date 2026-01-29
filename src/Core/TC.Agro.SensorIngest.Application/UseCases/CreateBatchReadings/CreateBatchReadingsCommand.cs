namespace TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings
{
    public sealed record CreateBatchReadingsCommand(
        IReadOnlyList<SensorReadingInput> Readings) : IBaseCommand<CreateBatchReadingsResponse>;

    public sealed record SensorReadingInput(
        string SensorId,
        Guid PlotId,
        DateTime Timestamp,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel);
}
