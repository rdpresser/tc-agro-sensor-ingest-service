namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    public sealed class GetAlertListQueryHandler
    {
        private readonly IAlertReadStore _readStore;
        private readonly IFusionCache _cache;
        private readonly ILogger<GetAlertListQueryHandler> _logger;

        public GetAlertListQueryHandler(
            IAlertReadStore readStore,
            IFusionCache cache,
            ILogger<GetAlertListQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetAlertListResponse>> Handle(
            GetAlertListQuery query,
            CancellationToken ct)
        {
            var cacheKey = $"{AppConstants.CacheKeys.AlertListPrefix}status:{query.Status ?? "all"}";

            var alerts = await _cache.GetOrSetAsync(
                cacheKey,
                async _ => await _readStore.GetAlertsAsync(query.Status, ct).ConfigureAwait(false),
                options => options.SetDuration(TimeSpan.FromSeconds(AppConstants.CacheTtlSeconds)),
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} alerts with Status={Status}",
                alerts?.Count ?? 0,
                query.Status ?? "all");

            var response = new GetAlertListResponse(alerts ?? []);
            return Result.Success(response);
        }
    }
}
