using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    public sealed record GetReadingsHistoryQuery : ICachedQuery<PaginatedResponse<GetReadingsHistoryResponse>>
    {
        public Guid SensorId { get; init; }
        public int Days { get; init; } = 7;
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetReadingsHistoryQuery-{SensorId}-{Days}-{PageNumber}-{PageSize}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Readings,
            Abstractions.CacheTags.ReadingsHistory
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetReadingsHistoryQuery-{SensorId}-{Days}-{PageNumber}-{PageSize}-{cacheKey}";
    }
}
