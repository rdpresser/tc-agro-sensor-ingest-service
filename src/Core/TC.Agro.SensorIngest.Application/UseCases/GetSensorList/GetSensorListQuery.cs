namespace TC.Agro.SensorIngest.Application.UseCases.GetSensorList
{
    public sealed record GetSensorListQuery : ICachedQuery<GetSensorListResponse>
    {
        public Guid? PlotId { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetSensorListQuery-{PlotId?.ToString() ?? "all"}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Sensors,
            CacheTagCatalog.SensorList
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetSensorListQuery-{PlotId?.ToString() ?? "all"}-{cacheKey}";
    }
}
