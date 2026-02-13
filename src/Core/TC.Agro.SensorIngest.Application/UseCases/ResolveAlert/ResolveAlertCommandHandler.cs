namespace TC.Agro.SensorIngest.Application.UseCases.ResolveAlert
{
    internal sealed class ResolveAlertCommandHandler
        : BaseCommandHandler<ResolveAlertCommand, ResolveAlertResponse, AlertAggregate, IAlertAggregateRepository>
    {
        public ResolveAlertCommandHandler(
            IAlertAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<ResolveAlertCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
        }

        protected override async Task<Result<AlertAggregate>> MapAsync(ResolveAlertCommand command, CancellationToken ct)
        {
            var alert = await Repository.GetByIdAsync(command.AlertId, ct).ConfigureAwait(false);

            if (alert is null)
                return Result.NotFound($"Alert with ID '{command.AlertId}' not found.");

            var resolveResult = alert.Resolve();
            if (!resolveResult.IsSuccess)
                return Result.Error(new ErrorList(resolveResult.Errors.ToArray()));

            return Result.Success(alert);
        }

        protected override Task PersistAsync(AlertAggregate aggregate, CancellationToken ct)
            => Task.CompletedTask; // Entity is already tracked by EF

        protected override Task<ResolveAlertResponse> BuildResponseAsync(AlertAggregate aggregate, CancellationToken ct)
            => Task.FromResult(new ResolveAlertResponse(
                Id: aggregate.Id,
                Status: aggregate.Status.Value,
                ResolvedAt: aggregate.ResolvedAt));
    }
}
