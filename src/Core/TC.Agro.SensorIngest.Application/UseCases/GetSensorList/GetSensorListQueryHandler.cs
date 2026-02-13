namespace TC.Agro.SensorIngest.Application.UseCases.GetSensorList
{
    internal sealed class GetSensorListQueryHandler : BaseQueryHandler<GetSensorListQuery, GetSensorListResponse>
    {
        private readonly ISensorReadStore _readStore;
        private readonly ILogger<GetSensorListQueryHandler> _logger;

        public GetSensorListQueryHandler(
            ISensorReadStore readStore,
            ILogger<GetSensorListQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<GetSensorListResponse>> ExecuteAsync(
            GetSensorListQuery query,
            CancellationToken ct = default)
        {
            var sensors = await _readStore.GetSensorsAsync(query.PlotId, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} sensors for PlotId={PlotId}",
                sensors.Count,
                query.PlotId?.ToString() ?? "all");

            return Result.Success(new GetSensorListResponse(sensors));
        }
    }
}
