namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed class GetLatestReadingsQueryHandler
    {
        private readonly ISensorReadingReadStore _readStore;
        private readonly IFusionCache _cache;
        private readonly ILogger<GetLatestReadingsQueryHandler> _logger;

        public GetLatestReadingsQueryHandler(
            ISensorReadingReadStore readStore,
            IFusionCache cache,
            ILogger<GetLatestReadingsQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetLatestReadingsResponse>> Handle(
            GetLatestReadingsQuery query,
            CancellationToken ct)
        {
            var limit = Math.Min(query.Limit, AppConstants.MaxReadLimit);
            var cacheKey = BuildCacheKey(query.SensorId, query.PlotId, limit);

            var readings = await _cache.GetOrSetAsync(
                cacheKey,
                async _ => await _readStore.GetLatestReadingsAsync(
                    sensorId: query.SensorId,
                    plotId: query.PlotId,
                    limit: limit,
                    cancellationToken: ct).ConfigureAwait(false),
                options => options.SetDuration(TimeSpan.FromSeconds(AppConstants.CacheTtlSeconds)),
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} latest readings for SensorId={SensorId}, PlotId={PlotId}",
                readings?.Count() ?? 0,
                query.SensorId ?? "all",
                query.PlotId?.ToString() ?? "all");

            var response = new GetLatestReadingsResponse(readings?.ToList() ?? []);
            return Result.Success(response);
        }

        private static string BuildCacheKey(string? sensorId, Guid? plotId, int limit)
        {
            var key = AppConstants.CacheKeys.LatestReadingsPrefix;
            if (!string.IsNullOrEmpty(sensorId))
                key += $"sensor:{sensorId}:";
            if (plotId.HasValue)
                key += $"plot:{plotId}:";
            key += $"limit:{limit}";
            return key;
        }
    }
}
