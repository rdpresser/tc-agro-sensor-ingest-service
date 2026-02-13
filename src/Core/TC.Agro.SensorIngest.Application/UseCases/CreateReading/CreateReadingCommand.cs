namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    public sealed record CreateReadingCommand(
        Guid SensorId,
        Guid PlotId,
        DateTime Timestamp,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel) : IBaseCommand<CreateReadingResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Readings,
            CacheTagCatalog.Dashboard
        ];
    }
}
