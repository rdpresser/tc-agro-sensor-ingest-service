using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record GetLatestReadingsQuery : ICachedQuery<PaginatedResponse<GetLatestReadingsResponse>>
    {
        public Guid? SensorId { get; init; }
        public Guid? PlotId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetLatestReadingsQuery-{SensorId}-{PlotId}-{PageNumber}-{PageSize}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Readings,
            Abstractions.CacheTags.ReadingsLatest
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetLatestReadingsQuery-{SensorId}-{PlotId}-{PageNumber}-{PageSize}-{cacheKey}";
    }
}
