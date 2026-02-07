namespace TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats
{
    public sealed class GetDashboardStatsQueryHandler
    {
        private readonly ISensorReadStore _sensorReadStore;
        private readonly IAlertReadStore _alertReadStore;
        private readonly IFusionCache _cache;
        private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

        public GetDashboardStatsQueryHandler(
            ISensorReadStore sensorReadStore,
            IAlertReadStore alertReadStore,
            IFusionCache cache,
            ILogger<GetDashboardStatsQueryHandler> logger)
        {
            _sensorReadStore = sensorReadStore ?? throw new ArgumentNullException(nameof(sensorReadStore));
            _alertReadStore = alertReadStore ?? throw new ArgumentNullException(nameof(alertReadStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<DashboardStatsResponse>> Handle(
            GetDashboardStatsQuery query,
            CancellationToken ct)
        {
            var response = await _cache.GetOrSetAsync(
                AppConstants.CacheKeys.DashboardStatsKey,
                async _ =>
                {
                    var sensorCount = await _sensorReadStore.CountAsync(ct).ConfigureAwait(false);
                    var alertCount = await _alertReadStore.CountPendingAsync(ct).ConfigureAwait(false);

                    // Properties and Plots return 0 until integration with farm-service via HTTP client
                    return new DashboardStatsResponse(
                        Properties: 0,
                        Plots: 0,
                        Sensors: sensorCount,
                        Alerts: alertCount);
                },
                options => options.SetDuration(TimeSpan.FromSeconds(AppConstants.CacheTtlSeconds)),
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Dashboard stats: Sensors={Sensors}, Alerts={Alerts}",
                response.Sensors,
                response.Alerts);

            return Result.Success(response);
        }
    }
}
