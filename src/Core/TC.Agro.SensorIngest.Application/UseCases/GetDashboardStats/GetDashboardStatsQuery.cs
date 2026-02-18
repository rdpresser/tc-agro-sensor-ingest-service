namespace TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats
{
    public sealed record GetDashboardStatsQuery : ICachedQuery<GetDashboardStatsResponse>
    {
        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? "GetDashboardStatsQuery";
        public TimeSpan? Duration { get; init; }
        public TimeSpan? DistributedCacheDuration { get; init; }

        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Dashboard,
            Abstractions.CacheTags.Sensors,
            Abstractions.CacheTags.Alerts
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetDashboardStatsQuery-{cacheKey}";
    }
}
