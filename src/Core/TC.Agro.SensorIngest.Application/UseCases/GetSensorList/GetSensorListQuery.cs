using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.UseCases.GetSensorList
{
    public sealed record GetSensorListQuery : ICachedQuery<PaginatedResponse<GetSensorListResponse>>
    {
        public Guid? PlotId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "createdat";
        public string SortDirection { get; init; } = "asc";
        public string? Filter { get; init; }
        public string? Status { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ??
            $"GetSensorListQuery-{PlotId}-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{Status}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            Abstractions.CacheTags.Sensors,
            Abstractions.CacheTags.SensorList
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey =
                $"GetSensorListQuery-{PlotId}-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{Status}-{cacheKey}";
    }
}
