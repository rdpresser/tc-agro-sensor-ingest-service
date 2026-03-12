using TC.Agro.Contracts.Events.SensorIngested;
using TC.Agro.SensorIngest.Application.Abstractions.Mappers;
using TC.Agro.SharedKernel.Domain.Events;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings
{
    internal sealed class CreateBatchReadingsCommandHandler
        : BaseHandler<CreateBatchReadingsCommand, CreateBatchReadingsResponse>
    {
        private readonly ISensorReadingRepository _repository;
        private readonly ITransactionalOutbox _outbox;
        private readonly ISensorSnapshotStore _sensorSnapshotStore;
        private readonly IUserContext _userContext;
        private readonly ILogger<CreateBatchReadingsCommandHandler> _logger;

        public CreateBatchReadingsCommandHandler(
            ISensorReadingRepository repository,
            ITransactionalOutbox outbox,
            ISensorSnapshotStore sensorSnapshotStore,
            IUserContext userContext,
            ILogger<CreateBatchReadingsCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _sensorSnapshotStore = sensorSnapshotStore ?? throw new ArgumentNullException(nameof(sensorSnapshotStore));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<CreateBatchReadingsResponse>> ExecuteAsync(
            CreateBatchReadingsCommand command,
            CancellationToken ct = default)
        {
            var results = new List<BatchReadingResult>();
            var successfulAggregates = new List<SensorReadingAggregate>();

            var distinctSensorIds = command.Readings.Select(r => r.SensorId).Distinct();
            var activeSensors = await _sensorSnapshotStore.GetByIdsAsync(distinctSensorIds, ct).ConfigureAwait(false);

            foreach (var input in command.Readings)
            {
                if (!activeSensors.ContainsKey(input.SensorId))
                {
                    _logger.LogWarning(
                        "Rejected batch reading for unknown sensor {SensorId}",
                        input.SensorId);

                    results.Add(new BatchReadingResult(
                        SensorReadingId: null,
                        SensorId: input.SensorId,
                        Success: false,
                        ErrorMessage: $"Sensor with ID '{input.SensorId}' is not registered."));
                    continue;
                }

                var aggregateResult = SensorReadingAggregate.Create(
                    sensorId: input.SensorId,
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
                        SensorReadingId: aggregateResult.Value.Id,
                        SensorId: input.SensorId,
                        Success: true));
                }
                else
                {
                    var errorMessage = string.Join("; ", aggregateResult.ValidationErrors.Select(e => e.ErrorMessage));
                    results.Add(new BatchReadingResult(
                        SensorReadingId: null,
                        SensorId: input.SensorId,
                        Success: false,
                        ErrorMessage: errorMessage));
                }
            }

            if (successfulAggregates.Count > 0)
            {
                await _repository.AddRangeAsync(successfulAggregates, ct).ConfigureAwait(false);

                var mappings = new Dictionary<Type, Func<BaseDomainEvent, SensorIngestedIntegrationEvent>>
                {
                    {
                        typeof(SensorReadingAggregate.SensorReadingCreatedDomainEvent),
                        domainEvent => ToIntegrationEvent((SensorReadingAggregate.SensorReadingCreatedDomainEvent)domainEvent)
                    }
                };

                foreach (var aggregate in successfulAggregates)
                {
                    var integrationEvents = aggregate.UncommittedEvents
                        .MapToIntegrationEvents(
                            aggregate: aggregate,
                            userContext: _userContext,
                            handlerName: nameof(CreateBatchReadingsCommandHandler),
                            mappings: mappings)
                        .ToList();

                    foreach (var integrationEvent in integrationEvents)
                    {
                        await _outbox.EnqueueAsync(integrationEvent, ct).ConfigureAwait(false);
                    }
                }

                await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);
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

        private static SensorIngestedIntegrationEvent ToIntegrationEvent(
            SensorReadingAggregate.SensorReadingCreatedDomainEvent createdEvent)
            => new(
                SensorReadingId: createdEvent.AggregateId,
                SensorId: createdEvent.SensorId,
                Time: createdEvent.Time,
                Temperature: createdEvent.Temperature,
                Humidity: createdEvent.Humidity,
                SoilMoisture: createdEvent.SoilMoisture,
                Rainfall: createdEvent.Rainfall,
                BatteryLevel: createdEvent.BatteryLevel,
                OccurredOn: createdEvent.OccurredOn);
    }
}
