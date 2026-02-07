namespace TC.Agro.SensorIngest.Application.UseCases.ResolveAlert
{
    public sealed class ResolveAlertCommandHandler
    {
        private readonly IAlertAggregateRepository _repository;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<ResolveAlertCommandHandler> _logger;

        public ResolveAlertCommandHandler(
            IAlertAggregateRepository repository,
            ITransactionalOutbox outbox,
            ILogger<ResolveAlertCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ResolveAlertResponse>> Handle(
            ResolveAlertCommand command,
            CancellationToken ct)
        {
            var alert = await _repository.GetByIdAsync(command.AlertId, ct).ConfigureAwait(false);

            if (alert is null)
                return Result.NotFound($"Alert with ID '{command.AlertId}' not found.");

            var resolveResult = alert.Resolve();
            if (!resolveResult.IsSuccess)
                return Result.Error(resolveResult.Errors.ToArray());

            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Alert {AlertId} resolved for sensor {SensorId}",
                alert.Id,
                alert.SensorId);

            return Result.Success(new ResolveAlertResponse(
                Id: alert.Id,
                Status: alert.Status.Value,
                ResolvedAt: alert.ResolvedAt));
        }
    }
}
