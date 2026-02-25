using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    internal sealed class GetLatestReadingsQueryHandler : BaseQueryHandler<GetLatestReadingsQuery, PaginatedResponse<GetLatestReadingsResponse>>
    {
        private readonly ISensorReadingReadStore _readStore;
        private readonly ILogger<GetLatestReadingsQueryHandler> _logger;

        public GetLatestReadingsQueryHandler(
            ISensorReadingReadStore readStore,
            ILogger<GetLatestReadingsQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<GetLatestReadingsResponse>>> ExecuteAsync(
            GetLatestReadingsQuery query,
            CancellationToken ct = default)
        {
            var maxPageSize = AppConstants.MaxReadLimit;
            var requestedPageSize = query.PageSize;
            var normalizedPageSize = Math.Clamp(requestedPageSize, 1, maxPageSize);
            var normalizedPageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

            var normalizedQuery = query with
            {
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize
            };

            var (readings, totalCount) = await _readStore.GetLatestReadingsAsync(
                normalizedQuery,
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} latest readings (Total={TotalCount}) for SensorId={SensorId}, PlotId={PlotId}, Page={PageNumber}, PageSize={PageSize}",
                readings?.Count ?? 0,
                totalCount,
                normalizedQuery.SensorId,
                normalizedQuery.PlotId,
                normalizedQuery.PageNumber,
                normalizedQuery.PageSize);

            var response = new PaginatedResponse<GetLatestReadingsResponse>(
                data: readings is null ? [] : [.. readings],
                totalCount: totalCount,
                pageNumber: normalizedQuery.PageNumber,
                pageSize: normalizedQuery.PageSize);

            return Result.Success(response);
        }
    }
}
