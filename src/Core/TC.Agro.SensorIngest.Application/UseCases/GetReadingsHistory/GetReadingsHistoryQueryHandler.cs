namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    internal sealed class GetReadingsHistoryQueryHandler : BaseQueryHandler<GetReadingsHistoryQuery, GetReadingsHistoryResponse>
    {
        private readonly ISensorReadingReadStore _readStore;
        private readonly ILogger<GetReadingsHistoryQueryHandler> _logger;

        public GetReadingsHistoryQueryHandler(
            ISensorReadingReadStore readStore,
            ILogger<GetReadingsHistoryQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<GetReadingsHistoryResponse>> ExecuteAsync(
            GetReadingsHistoryQuery query,
            CancellationToken ct = default)
        {
            var days = Math.Clamp(query.Days, 1, 30);
            var from = DateTime.UtcNow.AddDays(-days);
            var to = DateTime.UtcNow;

            var readings = await _readStore.GetHistoryAsync(
                query.SensorId,
                from,
                to,
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} readings history for SensorId={SensorId}, Days={Days}",
                readings?.Count() ?? 0,
                query.SensorId,
                days);

            return Result.Success(new GetReadingsHistoryResponse(readings ?? []));
        }
    }
}
