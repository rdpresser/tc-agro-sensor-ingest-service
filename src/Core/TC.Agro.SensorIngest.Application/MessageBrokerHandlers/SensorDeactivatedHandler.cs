using TC.Agro.Contracts.Events.Farm;

namespace TC.Agro.SensorIngest.Application.MessageBrokerHandlers
{
    /// <summary>
    /// Handles SensorDeactivatedIntegrationEvent from the Farm microservice.
    /// 
    /// When a sensor is permanently deactivated (soft-deleted) in Farm Service,
    /// this handler marks the sensor as inactive in Sensor-Ingest service.
    /// 
    /// Side effects:
    /// - Sensor snapshot marked IsActive = false
    /// - Sensor aggregate marked IsActive = false
    /// - No new sensor readings will be accepted
    /// - Alert rules stop processing for this sensor
    /// - Historical data is preserved (not physically deleted)
    /// 
    /// Idempotency:
    /// - If sensor not found, skip silently
    /// - If already deactivated, skip silently
    /// </summary>
    public sealed class SensorDeactivatedHandler : IWolverineHandler
    {
        private readonly ISensorAggregateRepository _sensorStore;
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SensorDeactivatedHandler> _logger;

        public SensorDeactivatedHandler(
            ISensorAggregateRepository sensorStore,
            ISensorSnapshotStore snapshotStore,
            IUnitOfWork unitOfWork,
            ILogger<SensorDeactivatedHandler> logger)
        {
            _sensorStore = sensorStore ?? throw new ArgumentNullException(nameof(sensorStore));
            _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(
            EventContext<SensorDeactivatedIntegrationEvent> @event,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var evt = @event.EventData;

            try
            {
                _logger.LogInformation(
                    "🔴 Processing SensorDeactivatedIntegrationEvent for SensorId: {SensorId}. " +
                    "Reason: {Reason}. EventId: {EventId}",
                    evt.SensorId,
                    evt.Reason,
                    evt.EventId);

                // IDEMPOTENCY: Load sensor by SensorId
                var sensor = await _sensorStore.GetBySensorIdAsync(evt.SensorId, cancellationToken)
                    .ConfigureAwait(false);

                if (sensor == null)
                {
                    _logger.LogWarning(
                        "⚠️ Sensor {SensorId} not found. Skipping deactivation (idempotent). EventId: {EventId}",
                        evt.SensorId,
                        evt.EventId);
                    return;  // Idempotent: sensor may not have synced yet or already deleted
                }

                // IDEMPOTENCY: Check if already deactivated
                if (!sensor.IsActive)
                {
                    _logger.LogInformation(
                        "✓ Sensor already deactivated (duplicate event). SensorId: {SensorId}. Skipping. EventId: {EventId}",
                        evt.SensorId,
                        evt.EventId);
                    return;  // Idempotent
                }

                // DEACTIVATE: Mark sensor as inactive
                sensor.Deactivate();

                // SYNC SNAPSHOT: Mark snapshot as inactive
                var snapshot = await _snapshotStore.GetByIdAsync(evt.SensorId, cancellationToken)
                    .ConfigureAwait(false);

                if (snapshot != null)
                {
                    snapshot.Delete();
                    await _snapshotStore.UpdateAsync(snapshot, cancellationToken).ConfigureAwait(false);
                }

                // PERSIST: Save both to database
                await _sensorStore.UpdateAsync(sensor, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "✅ Sensor {SensorId} deactivated successfully. Reason: {Reason}. EventId: {EventId}",
                    evt.SensorId,
                    evt.Reason,
                    evt.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error processing SensorDeactivatedIntegrationEvent: SensorId={SensorId}, " +
                    "EventId={EventId}, CorrelationId={CorrelationId}, Reason={Reason}. " +
                    "Exception={ExceptionType}. Will retry.",
                    evt.SensorId,
                    evt.EventId,
                    @event.CorrelationId,
                    evt.Reason,
                    ex.GetType().Name);
                throw new InvalidOperationException(
                    $"Failed to process SensorDeactivatedIntegrationEvent for SensorId: {evt.SensorId}, " +
                    $"EventId: {evt.EventId}. Check logs for details.",
                    ex);  // Let Wolverine retry with wrapped exception
            }
        }
    }
}
