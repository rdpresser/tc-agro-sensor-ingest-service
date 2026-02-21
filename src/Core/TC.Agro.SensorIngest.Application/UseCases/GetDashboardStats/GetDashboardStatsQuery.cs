namespace TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats
{
    public sealed record GetDashboardStatsQuery : ICachedQuery<GetDashboardStatsResponse>
    {
        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? "GetDashboardStatsQuery";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Dashboard,
            Abstractions.CacheTags.Alerts
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetDashboardStatsQuery-{cacheKey}";
    }
}
