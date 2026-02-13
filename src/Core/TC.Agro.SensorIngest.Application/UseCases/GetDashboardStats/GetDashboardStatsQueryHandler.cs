namespace TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats
{
    internal sealed class GetDashboardStatsQueryHandler : BaseQueryHandler<GetDashboardStatsQuery, DashboardStatsResponse>
    {
        private readonly ISensorReadStore _sensorReadStore;
        private readonly IAlertReadStore _alertReadStore;
        private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

        public GetDashboardStatsQueryHandler(
            ISensorReadStore sensorReadStore,
            IAlertReadStore alertReadStore,
            ILogger<GetDashboardStatsQueryHandler> logger)
        {
            _sensorReadStore = sensorReadStore ?? throw new ArgumentNullException(nameof(sensorReadStore));
            _alertReadStore = alertReadStore ?? throw new ArgumentNullException(nameof(alertReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<DashboardStatsResponse>> ExecuteAsync(
            GetDashboardStatsQuery query,
            CancellationToken ct = default)
        {
            var sensorCount = await _sensorReadStore.CountAsync(ct).ConfigureAwait(false);
            var alertCount = await _alertReadStore.CountPendingAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Dashboard stats: Sensors={Sensors}, Alerts={Alerts}",
                sensorCount,
                alertCount);

            // Properties and Plots return 0 until integration with farm-service via HTTP client
            return Result.Success(new DashboardStatsResponse(
                Properties: 0,
                Plots: 0,
                Sensors: sensorCount,
                Alerts: alertCount));
        }
    }
}
