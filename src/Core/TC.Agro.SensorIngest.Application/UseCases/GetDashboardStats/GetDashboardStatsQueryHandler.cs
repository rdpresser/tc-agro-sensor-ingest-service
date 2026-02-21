namespace TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats
{
    internal sealed class GetDashboardStatsQueryHandler : BaseQueryHandler<GetDashboardStatsQuery, GetDashboardStatsResponse>
    {
        private readonly IAlertReadStore _alertReadStore;
        private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

        public GetDashboardStatsQueryHandler(
            IAlertReadStore alertReadStore,
            ILogger<GetDashboardStatsQueryHandler> logger)
        {
            _alertReadStore = alertReadStore ?? throw new ArgumentNullException(nameof(alertReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<GetDashboardStatsResponse>> ExecuteAsync(
            GetDashboardStatsQuery query,
            CancellationToken ct = default)
        {
            var alertCount = await _alertReadStore.CountPendingAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Dashboard stats: Alerts={Alerts}",
                alertCount);

            // Properties, Plots and Sensors return 0 until integration with farm-service via HTTP client
            return Result.Success(new GetDashboardStatsResponse(
                Properties: 0,
                Plots: 0,
                Sensors: 0,
                Alerts: alertCount));
        }
    }
}
