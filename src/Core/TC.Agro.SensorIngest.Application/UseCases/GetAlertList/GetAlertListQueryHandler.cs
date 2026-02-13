using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    internal sealed class GetAlertListQueryHandler : BaseQueryHandler<GetAlertListQuery, PaginatedResponse<GetAlertListResponse>>
    {
        private readonly IAlertReadStore _readStore;
        private readonly ILogger<GetAlertListQueryHandler> _logger;

        public GetAlertListQueryHandler(
            IAlertReadStore readStore,
            ILogger<GetAlertListQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<GetAlertListResponse>>> ExecuteAsync(
            GetAlertListQuery query,
            CancellationToken ct = default)
        {
            var (alerts, totalCount) = await _readStore.GetAlertsAsync(query, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} alerts with Status={Status}",
                alerts.Count, query.Status);

            var response = new PaginatedResponse<GetAlertListResponse>(
                data: [.. alerts],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
