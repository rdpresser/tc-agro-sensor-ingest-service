namespace TC.Agro.SensorIngest.Application.UseCases.GetSensorList
{
    public sealed class GetSensorListQueryHandler
    {
        private readonly ISensorReadStore _readStore;
        private readonly IFusionCache _cache;
        private readonly ILogger<GetSensorListQueryHandler> _logger;

        public GetSensorListQueryHandler(
            ISensorReadStore readStore,
            IFusionCache cache,
            ILogger<GetSensorListQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetSensorListResponse>> Handle(
            GetSensorListQuery query,
            CancellationToken ct)
        {
            var cacheKey = $"{AppConstants.CacheKeys.SensorListPrefix}plot:{query.PlotId?.ToString() ?? "all"}";

            var sensors = await _cache.GetOrSetAsync(
                cacheKey,
                async _ => await _readStore.GetSensorsAsync(query.PlotId, ct).ConfigureAwait(false),
                options => options.SetDuration(TimeSpan.FromSeconds(AppConstants.CacheTtlSeconds)),
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} sensors for PlotId={PlotId}",
                sensors?.Count ?? 0,
                query.PlotId?.ToString() ?? "all");

            var response = new GetSensorListResponse(sensors ?? []);
            return Result.Success(response);
        }
    }
}
