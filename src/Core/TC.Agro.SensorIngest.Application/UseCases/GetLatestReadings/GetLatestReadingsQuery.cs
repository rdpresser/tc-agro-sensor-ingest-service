namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record GetLatestReadingsQuery : ICachedQuery<GetLatestReadingsResponse>
    {
        public string? SensorId { get; init; }
        public Guid? PlotId { get; init; }
        public int Limit { get; init; } = 10;

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetLatestReadingsQuery-{SensorId ?? "all"}-{PlotId?.ToString() ?? "all"}-{Limit}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Readings,
            CacheTagCatalog.ReadingsLatest
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetLatestReadingsQuery-{SensorId ?? "all"}-{PlotId?.ToString() ?? "all"}-{Limit}-{cacheKey}";
    }
}
