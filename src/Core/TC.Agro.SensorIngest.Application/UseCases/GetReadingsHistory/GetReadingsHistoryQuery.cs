namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    public sealed record GetReadingsHistoryQuery : ICachedQuery<GetReadingsHistoryResponse>
    {
        public string SensorId { get; init; } = default!;
        public int Days { get; init; } = 7;

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetReadingsHistoryQuery-{SensorId}-{Days}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Readings,
            CacheTagCatalog.ReadingsHistory
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetReadingsHistoryQuery-{SensorId}-{Days}-{cacheKey}";
    }
}
