using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    internal sealed class GetReadingsHistoryQueryHandler : BaseQueryHandler<GetReadingsHistoryQuery, PaginatedResponse<GetReadingsHistoryResponse>>
    {
        private readonly ISensorReadingReadStore _readStore;
        private readonly ILogger<GetReadingsHistoryQueryHandler> _logger;

        public GetReadingsHistoryQueryHandler(
            ISensorReadingReadStore readStore,
            ILogger<GetReadingsHistoryQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<GetReadingsHistoryResponse>>> ExecuteAsync(
            GetReadingsHistoryQuery query,
            CancellationToken ct = default)
        {
            var days = Math.Clamp(query.Days, 1, 30);
            var normalizedPageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var normalizedPageSize = Math.Clamp(query.PageSize, 1, AppConstants.MaxReadLimit);

            var normalizedQuery = query with
            {
                Days = days,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize
            };

            var (readings, totalCount) = await _readStore.GetHistoryAsync(
                normalizedQuery,
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} readings history (Total={TotalCount}) for SensorId={SensorId}, Days={Days}, Page={PageNumber}, PageSize={PageSize}",
                readings?.Count ?? 0,
                totalCount,
                normalizedQuery.SensorId,
                normalizedQuery.Days,
                normalizedQuery.PageNumber,
                normalizedQuery.PageSize);

            var response = new PaginatedResponse<GetReadingsHistoryResponse>(
                data: readings is null ? [] : [.. readings],
                totalCount: totalCount,
                pageNumber: normalizedQuery.PageNumber,
                pageSize: normalizedQuery.PageSize);

            return Result.Success(response);
        }
    }
}
