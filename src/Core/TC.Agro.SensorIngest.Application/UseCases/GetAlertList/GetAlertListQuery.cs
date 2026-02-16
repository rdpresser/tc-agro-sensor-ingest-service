using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    public sealed record GetAlertListQuery : ICachedQuery<PaginatedResponse<GetAlertListResponse>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "name";
        public string SortDirection { get; init; } = "asc";
        public string? Filter { get; init; }
        public string? Status { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetAlertListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{Status}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Alerts,
            Abstractions.CacheTags.AlertList
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetAlertListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{Status}-{cacheKey}";
    }
}
