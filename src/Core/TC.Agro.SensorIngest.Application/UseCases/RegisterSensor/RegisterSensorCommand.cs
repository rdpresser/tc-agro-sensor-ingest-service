namespace TC.Agro.SensorIngest.Application.UseCases.RegisterSensor
{
    public sealed record RegisterSensorCommand(
        string SensorId,
        Guid PlotId,
        string PlotName,
        double Battery) : IBaseCommand<RegisterSensorResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Sensors,
            CacheTagCatalog.Dashboard
        ];
    }
}
