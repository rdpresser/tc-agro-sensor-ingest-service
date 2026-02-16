using TC.Agro.Contracts.Events.SensorIngested;

namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    internal sealed class CreateReadingCommandHandler
        : BaseCommandHandler<CreateReadingCommand, CreateReadingResponse, SensorReadingAggregate, ISensorReadingRepository>
    {
        private readonly ILogger<CreateReadingCommandHandler> _logger;
        private readonly ISensorHubNotifier _hubNotifier;

        public CreateReadingCommandHandler(
            ISensorReadingRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ISensorHubNotifier hubNotifier,
            ILogger<CreateReadingCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubNotifier = hubNotifier ?? throw new ArgumentNullException(nameof(hubNotifier));
        }

        protected override Task<Result<SensorReadingAggregate>> MapAsync(CreateReadingCommand command, CancellationToken ct)
        {
            var aggregateResult = CreateReadingMapper.ToAggregate(command);
            return Task.FromResult(aggregateResult);
        }

        protected override Task<Result> ValidateAsync(SensorReadingAggregate aggregate, CancellationToken ct)
        {
            // Additional business validation can be added here
            // e.g., verify sensor exists in Farm service, verify plot belongs to user, etc.
            return Task.FromResult(Result.Success());
        }

        protected override async Task PublishIntegrationEventsAsync(SensorReadingAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    handlerName: nameof(CreateReadingCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, SensorIngestedIntegrationEvent>>
                    {
                        { typeof(SensorReadingAggregate.SensorReadingCreatedDomainEvent), e =>
                            CreateReadingMapper.ToIntegrationEvent((SensorReadingAggregate.SensorReadingCreatedDomainEvent)e) }
                    });

            foreach (var evt in integrationEvents)
            {
                await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for sensor reading {ReadingId} from sensor {SensorId}",
                integrationEvents.Count(),
                aggregate.Id,
                aggregate.SensorId);
        }

        protected override async Task<CreateReadingResponse> BuildResponseAsync(SensorReadingAggregate aggregate, CancellationToken ct)
        {
            await _hubNotifier.NotifySensorReadingAsync(
                aggregate.SensorId,
                aggregate.PlotId,
                aggregate.Temperature,
                aggregate.Humidity,
                aggregate.SoilMoisture,
                aggregate.Time,
                ct).ConfigureAwait(false);

            return CreateReadingMapper.FromAggregate(aggregate);
        }
    }
}
