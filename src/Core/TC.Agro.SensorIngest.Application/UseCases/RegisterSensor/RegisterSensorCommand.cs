namespace TC.Agro.SensorIngest.Application.UseCases.RegisterSensor
{
    public sealed record RegisterSensorCommand(
        Guid SensorId,
        Guid PlotId,
        string PlotName,
        double Battery) : IBaseCommand<RegisterSensorResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Sensors,
            Abstractions.CacheTags.Dashboard
        ];
    }
}
