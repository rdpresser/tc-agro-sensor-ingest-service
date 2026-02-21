using TC.Agro.Contracts.Events.SensorIngested;

namespace TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings
{
    internal sealed class CreateBatchReadingsCommandHandler
        : BaseHandler<CreateBatchReadingsCommand, CreateBatchReadingsResponse>
    {
        private readonly ISensorReadingRepository _repository;
        private readonly ITransactionalOutbox _outbox;
        private readonly ISensorSnapshotStore _sensorSnapshotStore;
        private readonly ILogger<CreateBatchReadingsCommandHandler> _logger;

        public CreateBatchReadingsCommandHandler(
            ISensorReadingRepository repository,
            ITransactionalOutbox outbox,
            ISensorSnapshotStore sensorSnapshotStore,
            ILogger<CreateBatchReadingsCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _sensorSnapshotStore = sensorSnapshotStore ?? throw new ArgumentNullException(nameof(sensorSnapshotStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<CreateBatchReadingsResponse>> ExecuteAsync(
            CreateBatchReadingsCommand command,
            CancellationToken ct = default)
        {
            var results = new List<BatchReadingResult>();
            var successfulAggregates = new List<SensorReadingAggregate>();
            var sensorExistsCache = new Dictionary<Guid, bool>();

            foreach (var input in command.Readings)
            {
                if (!sensorExistsCache.TryGetValue(input.SensorId, out var sensorExists))
                {
                    sensorExists = await _sensorSnapshotStore.ExistsAsync(input.SensorId, ct).ConfigureAwait(false);
                    sensorExistsCache[input.SensorId] = sensorExists;
                }

                if (!sensorExists)
                {
                    _logger.LogWarning(
                        "Rejected batch reading for unknown sensor {SensorId}",
                        input.SensorId);

                    results.Add(new BatchReadingResult(
                        ReadingId: null,
                        SensorId: input.SensorId,
                        Success: false,
                        ErrorMessage: $"Sensor with ID '{input.SensorId}' is not registered."));
                    continue;
                }

                var aggregateResult = SensorReadingAggregate.Create(
                    sensorId: input.SensorId,
                    plotId: input.PlotId,
                    time: input.Timestamp,
                    temperature: input.Temperature,
                    humidity: input.Humidity,
                    soilMoisture: input.SoilMoisture,
                    rainfall: input.Rainfall,
                    batteryLevel: input.BatteryLevel);

                if (aggregateResult.IsSuccess)
                {
                    successfulAggregates.Add(aggregateResult.Value);
                    results.Add(new BatchReadingResult(
                        ReadingId: aggregateResult.Value.Id,
                        SensorId: input.SensorId,
                        Success: true));
                }
                else
                {
                    var errorMessage = string.Join("; ", aggregateResult.ValidationErrors.Select(e => e.ErrorMessage));
                    results.Add(new BatchReadingResult(
                        ReadingId: null,
                        SensorId: input.SensorId,
                        Success: false,
                        ErrorMessage: errorMessage));
                }
            }

            if (successfulAggregates.Count > 0)
            {
                await _repository.AddRangeAsync(successfulAggregates, ct).ConfigureAwait(false);

                foreach (var aggregate in successfulAggregates)
                {
                    foreach (var domainEvent in aggregate.UncommittedEvents)
                    {
                        if (domainEvent is SensorReadingAggregate.SensorReadingCreatedDomainEvent createdEvent)
                        {
                            var integrationEvent = new SensorIngestedIntegrationEvent(
                                SensorReadingId: createdEvent.AggregateId,
                                OccurredOn: createdEvent.OccurredOn,
                                SensorId: createdEvent.SensorId,
                                PlotId: createdEvent.PlotId,
                                Time: createdEvent.Time,
                                Temperature: createdEvent.Temperature,
                                Humidity: createdEvent.Humidity,
                                SoilMoisture: createdEvent.SoilMoisture,
                                Rainfall: createdEvent.Rainfall,
                                BatteryLevel: createdEvent.BatteryLevel);

                            await _outbox.EnqueueAsync(integrationEvent, ct).ConfigureAwait(false);
                        }
                    }
                }
            }

            _logger.LogInformation(
                "Batch processed: {ProcessedCount} successful, {FailedCount} failed out of {TotalCount} readings",
                successfulAggregates.Count,
                results.Count - successfulAggregates.Count,
                command.Readings.Count);

            return Result.Success(new CreateBatchReadingsResponse(
                ProcessedCount: successfulAggregates.Count,
                FailedCount: results.Count - successfulAggregates.Count,
                Results: results));
        }
    }
}
