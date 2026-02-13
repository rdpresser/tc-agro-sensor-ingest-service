namespace TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings
{
    public sealed record CreateBatchReadingsCommand(
        IReadOnlyList<SensorReadingInput> Readings) : IBaseCommand<CreateBatchReadingsResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Readings,
            CacheTagCatalog.Dashboard
        ];
    }

    public sealed record SensorReadingInput(
        Guid SensorId,
        Guid PlotId,
        DateTime Timestamp,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel);
}
