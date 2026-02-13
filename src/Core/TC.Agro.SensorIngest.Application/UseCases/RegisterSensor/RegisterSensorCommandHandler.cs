namespace TC.Agro.SensorIngest.Application.UseCases.RegisterSensor
{
    internal sealed class RegisterSensorCommandHandler
        : BaseCommandHandler<RegisterSensorCommand, RegisterSensorResponse, SensorAggregate, ISensorAggregateRepository>
    {
        public RegisterSensorCommandHandler(
            ISensorAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<RegisterSensorCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
        }

        protected override Task<Result<SensorAggregate>> MapAsync(RegisterSensorCommand command, CancellationToken ct)
        {
            var aggregateResult = RegisterSensorMapper.ToAggregate(command);
            return Task.FromResult(aggregateResult);
        }

        protected override async Task<Result> ValidateAsync(SensorAggregate aggregate, CancellationToken ct)
        {
            var exists = await Repository.SensorIdExistsAsync(aggregate.SensorId, ct).ConfigureAwait(false);
            if (exists)
                return Result.Error($"Sensor with ID '{aggregate.SensorId}' already exists.");

            return Result.Success();
        }

        protected override Task<RegisterSensorResponse> BuildResponseAsync(SensorAggregate aggregate, CancellationToken ct)
            => Task.FromResult(RegisterSensorMapper.FromAggregate(aggregate));
    }
}
