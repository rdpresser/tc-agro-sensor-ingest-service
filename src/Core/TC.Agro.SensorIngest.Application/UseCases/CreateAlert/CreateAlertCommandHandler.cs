namespace TC.Agro.SensorIngest.Application.UseCases.CreateAlert
{
    internal sealed class CreateAlertCommandHandler
        : BaseCommandHandler<CreateAlertCommand, CreateAlertResponse, AlertAggregate, IAlertAggregateRepository>
    {
        private readonly ISensorHubNotifier _hubNotifier;

        public CreateAlertCommandHandler(
            IAlertAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ISensorHubNotifier hubNotifier,
            ILogger<CreateAlertCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _hubNotifier = hubNotifier ?? throw new ArgumentNullException(nameof(hubNotifier));
        }

        protected override Task<Result<AlertAggregate>> MapAsync(CreateAlertCommand command, CancellationToken ct)
        {
            var aggregateResult = CreateAlertMapper.ToAggregate(command);
            return Task.FromResult(aggregateResult);
        }

        protected override Task<Result> ValidateAsync(AlertAggregate aggregate, CancellationToken ct)
        {
            return Task.FromResult(Result.Success());
        }

        protected override async Task<CreateAlertResponse> BuildResponseAsync(AlertAggregate aggregate, CancellationToken ct)
        {
            await _hubNotifier.NotifyNewAlertAsync(
                aggregate.Id,
                aggregate.Severity.Value,
                aggregate.Title,
                aggregate.Message,
                aggregate.PlotId,
                aggregate.PlotName,
                aggregate.SensorId,
                aggregate.Status.Value,
                aggregate.CreatedAt,
                ct).ConfigureAwait(false);

            return CreateAlertMapper.FromAggregate(aggregate);
        }
    }
}
