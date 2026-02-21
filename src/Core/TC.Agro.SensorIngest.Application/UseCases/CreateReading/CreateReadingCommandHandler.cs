using TC.Agro.Contracts.Events.SensorIngested;

namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    internal sealed class CreateReadingCommandHandler
        : BaseCommandHandler<CreateReadingCommand, CreateReadingResponse, SensorReadingAggregate, ISensorReadingRepository>
    {
        private readonly ILogger<CreateReadingCommandHandler> _logger;
        private readonly ISensorHubNotifier _hubNotifier;
        private readonly ISensorSnapshotStore _sensorSnapshotStore;
        private SensorSnapshot? _sensorSnapshot;

        public CreateReadingCommandHandler(
            ISensorReadingRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ISensorHubNotifier hubNotifier,
            ISensorSnapshotStore sensorSnapshotStore,
            ILogger<CreateReadingCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubNotifier = hubNotifier ?? throw new ArgumentNullException(nameof(hubNotifier));
            _sensorSnapshotStore = sensorSnapshotStore ?? throw new ArgumentNullException(nameof(sensorSnapshotStore));
        }

        protected override Task<Result<SensorReadingAggregate>> MapAsync(CreateReadingCommand command, CancellationToken ct)
        {
            var aggregateResult = CreateReadingMapper.ToAggregate(command);
            return Task.FromResult(aggregateResult);
        }

        protected override async Task<Result> ValidateAsync(SensorReadingAggregate aggregate, CancellationToken ct)
        {
            _sensorSnapshot = await _sensorSnapshotStore.GetByIdAsync(aggregate.SensorId, ct).ConfigureAwait(false);
            if (_sensorSnapshot is null || !_sensorSnapshot.IsActive)
            {
                _logger.LogWarning(
                    "Rejected reading for unknown sensor {SensorId}",
                    aggregate.SensorId);

                return Result.NotFound($"Sensor with ID '{aggregate.SensorId}' is not registered.");
            }

            return Result.Success();
        }

        protected override async Task PublishIntegrationEventsAsync(SensorReadingAggregate aggregate, CancellationToken ct)
        {
            var plotId = _sensorSnapshot!.PlotId;
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    handlerName: nameof(CreateReadingCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, SensorIngestedIntegrationEvent>>
                    {
                        { typeof(SensorReadingAggregate.SensorReadingCreatedDomainEvent), e =>
                            CreateReadingMapper.ToIntegrationEvent((SensorReadingAggregate.SensorReadingCreatedDomainEvent)e, plotId) }
                    });

            foreach (var evt in integrationEvents)
            {
                await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for sensor reading {SensorReadingId} from sensor {SensorId}",
                integrationEvents.Count(),
                aggregate.Id,
                aggregate.SensorId);
        }

        protected override async Task<CreateReadingResponse> BuildResponseAsync(SensorReadingAggregate aggregate, CancellationToken ct)
        {
            await _hubNotifier.NotifySensorReadingAsync(
                aggregate.SensorId,
                aggregate.Temperature,
                aggregate.Humidity,
                aggregate.SoilMoisture,
                aggregate.Time).ConfigureAwait(false);

            return CreateReadingMapper.FromAggregate(aggregate);
        }
    }
}
