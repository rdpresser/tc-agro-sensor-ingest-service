namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    internal sealed class GetAlertListQueryHandler : BaseQueryHandler<GetAlertListQuery, GetAlertListResponse>
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

        public override async Task<Result<GetAlertListResponse>> ExecuteAsync(
            GetAlertListQuery query,
            CancellationToken ct = default)
        {
            var alerts = await _readStore.GetAlertsAsync(query.Status, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} alerts with Status={Status}",
                alerts.Count,
                query.Status ?? "all");

            return Result.Success(new GetAlertListResponse(alerts));
        }
    }
}
