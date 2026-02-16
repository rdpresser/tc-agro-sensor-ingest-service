namespace TC.Agro.SensorIngest.Application.UseCases.ResolveAlert
{
    public sealed record ResolveAlertCommand(
        Guid AlertId) : IBaseCommand<ResolveAlertResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Alerts,
            Abstractions.CacheTags.Dashboard
        ];
    }
}
