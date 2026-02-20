using TC.Agro.Contracts.Events.Farm;

namespace TC.Agro.SensorIngest.Application.MessageBrokerHandlers
{
    /// <summary>
    /// Handles SensorOperationalStatusChangedIntegrationEvent from the Farm microservice.
    /// 
    /// When the Farm Service changes a sensor's operational status (Active, Maintenance, Faulty, Inactive),
    /// this handler synchronizes that state into the local SensorAggregate in Sensor-Ingest service.
    /// 
    /// This enables:
    /// - Operational status display in sensor readings
    /// - Alert rules to consider operator-set status (e.g., don't alert if Maintenance)
    /// - Audit trail of status changes initiated from Farm Service
    /// 
    /// Idempotency:
    /// - If sensor not found, skip silently
    /// - If status already applied, skip silently
    /// - CorrelationId + EventId prevent duplicate processing
    /// </summary>
    public sealed class SensorOperationalStatusChangedHandler : IWolverineHandler
    {
        private readonly ISensorAggregateRepository _sensorStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SensorOperationalStatusChangedHandler> _logger;

        public SensorOperationalStatusChangedHandler(
            ISensorAggregateRepository sensorStore,
            IUnitOfWork unitOfWork,
            ILogger<SensorOperationalStatusChangedHandler> logger)
        {
            _sensorStore = sensorStore ?? throw new ArgumentNullException(nameof(sensorStore));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(
            EventContext<SensorOperationalStatusChangedIntegrationEvent> @event,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var evt = @event.EventData;

            try
            {
                _logger.LogInformation(
                    "🔄 Processing SensorOperationalStatusChangedIntegrationEvent for SensorId: {SensorId}, " +
                    "Status: {PreviousStatus} → {NewStatus}, EventId: {EventId}, CorrelationId: {CorrelationId}",
                    evt.SensorId,
                    evt.PreviousStatus,
                    evt.NewStatus,
                    evt.EventId,
                    @event.CorrelationId);

                // IDEMPOTENCY: Validate event
                if (string.IsNullOrEmpty(evt.NewStatus))
                {
                    _logger.LogWarning(
                        "⚠️ Invalid event: NewStatus is empty. Skipping (idempotent). EventId: {EventId}",
                        evt.EventId);
                    return;  // Skip silently - this is idempotent
                }

                // IDEMPOTENCY: Load sensor by SensorId (from Farm)
                var sensor = await _sensorStore.GetBySensorIdAsync(evt.SensorId, cancellationToken)
                    .ConfigureAwait(false);

                if (sensor == null)
                {
                    _logger.LogWarning(
                        "⚠️ Sensor {SensorId} not found in Sensor-Ingest. Possibly deleted. " +
                        "Skipping update (idempotent). EventId: {EventId}",
                        evt.SensorId,
                        evt.EventId);
                    return;  // Idempotent: sensor may not have synced yet
                }

                // IDEMPOTENCY: Check if status already applied (within time window)
                if (sensor.OperationalStatus == evt.NewStatus &&
                    sensor.LastStatusChangeAt.HasValue &&
                    sensor.LastStatusChangeAt.Value >= evt.OccurredOn.Subtract(TimeSpan.FromSeconds(5)))
                {
                    _logger.LogInformation(
                        "✓ Status already applied (duplicate event). SensorId: {SensorId}, Status: {Status}, " +
                        "Skipping. EventId: {EventId}",
                        evt.SensorId,
                        evt.NewStatus,
                        evt.EventId);
                    return;  // Idempotent
                }

                // UPDATE: Apply operational status change
                sensor.UpdateOperationalStatus(
                    evt.NewStatus,
                    evt.Reason,
                    evt.OccurredOn,
                    evt.ChangedByUserId);

                // PERSIST: Save to database within transaction
                await _sensorStore.UpdateAsync(sensor, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "✅ Sensor {SensorId} operational status updated: {PreviousStatus} → {NewStatus}. " +
                    "Reason: {Reason}. EventId: {EventId}",
                    evt.SensorId,
                    evt.PreviousStatus,
                    evt.NewStatus,
                    evt.Reason ?? "Not provided",
                    evt.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error processing SensorOperationalStatusChangedIntegrationEvent: SensorId={SensorId}, " +
                    "EventId={EventId}, CorrelationId={CorrelationId}, Status={NewStatus}. " +
                    "Exception={ExceptionType}. Will retry.",
                    evt.SensorId,
                    evt.EventId,
                    @event.CorrelationId,
                    evt.NewStatus,
                    ex.GetType().Name);
                throw new InvalidOperationException(
                    $"Failed to process SensorOperationalStatusChangedIntegrationEvent for SensorId: {evt.SensorId}, " +
                    $"EventId: {evt.EventId}. Check logs for details.",
                    ex);  // Let Wolverine retry with wrapped exception
            }
        }
    }
}
