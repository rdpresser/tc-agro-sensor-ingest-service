namespace TC.Agro.SensorIngest.Application.UseCases.CreateAlert
{
    public sealed record CreateAlertCommand(
        string Severity,
        string Title,
        string Message,
        Guid PlotId,
        string PlotName,
        Guid SensorId) : IBaseCommand<CreateAlertResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Alerts,
            Abstractions.CacheTags.Dashboard
        ];
    }
}
