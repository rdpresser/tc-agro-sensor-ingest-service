using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.UseCases.GetSensorList
{
    internal sealed class GetSensorListQueryHandler : BaseQueryHandler<GetSensorListQuery, PaginatedResponse<GetSensorListResponse>>
    {
        private readonly ISensorReadStore _readStore;
        private readonly ILogger<GetSensorListQueryHandler> _logger;

        public GetSensorListQueryHandler(
            ISensorReadStore readStore,
            ILogger<GetSensorListQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<GetSensorListResponse>>> ExecuteAsync(
            GetSensorListQuery query,
            CancellationToken ct = default)
        {
            var (sensors, totalCount) = await _readStore.GetSensorsAsync(query, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} sensors for PlotId={PlotId}",
                sensors.Count,
                query.PlotId?.ToString() ?? "all");

            var response = new PaginatedResponse<GetSensorListResponse>(
                data: [.. sensors],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
