namespace TC.Agro.SensorIngest.Application.UseCases.ResolveAlert
{
    public sealed record ResolveAlertCommand(
        Guid AlertId) : IBaseCommand<ResolveAlertResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Alerts,
            CacheTagCatalog.Dashboard
        ];
    }
}
