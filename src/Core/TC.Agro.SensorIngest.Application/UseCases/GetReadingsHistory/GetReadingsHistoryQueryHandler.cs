namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    public sealed class GetReadingsHistoryQueryHandler
    {
        private readonly ISensorReadingReadStore _readStore;
        private readonly IFusionCache _cache;
        private readonly ILogger<GetReadingsHistoryQueryHandler> _logger;

        public GetReadingsHistoryQueryHandler(
            ISensorReadingReadStore readStore,
            IFusionCache cache,
            ILogger<GetReadingsHistoryQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetReadingsHistoryResponse>> Handle(
            GetReadingsHistoryQuery query,
            CancellationToken ct)
        {
            var days = Math.Clamp(query.Days, 1, 30);
            var from = DateTime.UtcNow.AddDays(-days);
            var to = DateTime.UtcNow;

            var cacheKey = $"{AppConstants.CacheKeys.LatestReadingsPrefix}history:{query.SensorId}:days:{days}";

            var readings = await _cache.GetOrSetAsync(
                cacheKey,
                async _ => await _readStore.GetHistoryAsync(
                    query.SensorId,
                    from,
                    to,
                    ct).ConfigureAwait(false),
                options => options.SetDuration(TimeSpan.FromSeconds(AppConstants.CacheTtlSeconds)),
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} readings history for SensorId={SensorId}, Days={Days}",
                readings?.Count() ?? 0,
                query.SensorId,
                days);

            var response = new GetReadingsHistoryResponse(readings?.ToList() ?? []);
            return Result.Success(response);
        }
    }
}
