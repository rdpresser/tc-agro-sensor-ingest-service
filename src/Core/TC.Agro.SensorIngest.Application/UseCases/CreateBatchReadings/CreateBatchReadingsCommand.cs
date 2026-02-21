namespace TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings
{
    public sealed record CreateBatchReadingsCommand(
        IReadOnlyList<SensorReadingInput> Readings) : IBaseCommand<CreateBatchReadingsResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Readings,
            Abstractions.CacheTags.Dashboard
        ];
    }

    public sealed record SensorReadingInput(
        Guid SensorId,
        DateTime Timestamp,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel);
}
