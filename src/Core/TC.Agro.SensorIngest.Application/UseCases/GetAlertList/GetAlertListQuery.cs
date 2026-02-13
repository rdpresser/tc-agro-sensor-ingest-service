namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    public sealed record GetAlertListQuery : ICachedQuery<GetAlertListResponse>
    {
        public string? Status { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetAlertListQuery-{Status ?? "all"}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Alerts,
            CacheTagCatalog.AlertList
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetAlertListQuery-{Status ?? "all"}-{cacheKey}";
    }
}
