namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record GetLatestReadingsQuery : ICachedQuery<GetLatestReadingsResponse>
    {
        public Guid? SensorId { get; init; }
        public Guid? PlotId { get; init; }
        public int Limit { get; init; } = 10;

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetLatestReadingsQuery-{SensorId}-{PlotId}-{Limit}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Readings,
            Abstractions.CacheTags.ReadingsLatest
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetLatestReadingsQuery-{SensorId}-{PlotId}-{Limit}-{cacheKey}";
    }
}
