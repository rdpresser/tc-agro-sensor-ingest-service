namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    internal sealed class GetLatestReadingsQueryHandler : BaseQueryHandler<GetLatestReadingsQuery, GetLatestReadingsResponse>
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

        public override async Task<Result<GetLatestReadingsResponse>> ExecuteAsync(
            GetLatestReadingsQuery query,
            CancellationToken ct = default)
        {
            var limit = Math.Min(query.Limit, AppConstants.MaxReadLimit);

            var readings = await _readStore.GetLatestReadingsAsync(
                sensorId: query.SensorId,
                plotId: query.PlotId,
                limit: limit,
                cancellationToken: ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} latest readings for SensorId={SensorId}, PlotId={PlotId}",
                readings?.Count() ?? 0,
                query.SensorId ?? "all",
                query.PlotId?.ToString() ?? "all");

            return Result.Success(new GetLatestReadingsResponse(readings?.ToList() ?? []));
        }
    }
}
