using TC.Agro.Contracts.Events.Farm;

namespace TC.Agro.SensorIngest.Application.MessageBrokerHandlers
{
    /// <summary>
    /// Handles sensor integration events from the Farm microservice and maintains
    /// the SensorSnapshot store accordingly.
    /// This class projects external sensor events into a read-optimized snapshot
    /// for the Sensor Ingest microservice.
    /// </summary>
    public class SensorSnapshotHandler : IWolverineHandler
    {
        private readonly ISensorSnapshotStore _store;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SensorSnapshotHandler> _logger;

        public SensorSnapshotHandler(ISensorSnapshotStore store, IUnitOfWork unitOfWork, ILogger<SensorSnapshotHandler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // -------------------------
        // Sensor Registered
        // -------------------------
        /// <summary>
        /// Handles the SensorRegisteredIntegrationEvent by creating a new SensorSnapshot.
        /// Saves the snapshot to the store.
        /// </summary>
        public async Task HandleAsync(EventContext<SensorRegisteredIntegrationEvent> @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var label = string.IsNullOrWhiteSpace(@event.EventData.Label)
                ? "Unnamed Sensor"
                : @event.EventData.Label;

            var snapshot = SensorSnapshot.Create(
                @event.EventData.SensorId,
                @event.EventData.OwnerId,
                @event.EventData.PropertyId,
                @event.EventData.PlotId,
                label,
                plotName: @event.EventData.PlotName,
                propertyName: @event.EventData.PropertyName,
                createdAt: @event.EventData.OccurredOn);

            await _store.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // -------------------------
        // Sensor Deactivated
        // -------------------------
        /// <summary>
        /// Handles the SensorDeactivatedIntegrationEvent by marking the SensorSnapshot as inactive (soft-delete).
        /// </summary>
        public async Task HandleAsync(EventContext<SensorDeactivatedIntegrationEvent> @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var sensorId = @event.EventData.SensorId;

            _logger.LogInformation("Handling SensorDeactivatedIntegrationEvent for SensorId {SensorId}. Reason: {Reason}",
                sensorId, @event.EventData.Reason);

            await _store.DeleteAsync(sensorId, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // -------------------------
        // Sensor Operational Status Changed
        // -------------------------
        /// <summary>
        /// Handles the SensorOperationalStatusChangedIntegrationEvent by activating or deactivating
        /// the SensorSnapshot based on the new operational status.
        /// </summary>
        public async Task HandleAsync(EventContext<SensorOperationalStatusChangedIntegrationEvent> @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var sensorId = @event.EventData.SensorId;
            var newStatus = @event.EventData.NewStatus;

            _logger.LogInformation(
                "Handling SensorOperationalStatusChangedIntegrationEvent for SensorId {SensorId}. Status: {PreviousStatus} -> {NewStatus}",
                sensorId, @event.EventData.PreviousStatus, newStatus);

            if (string.Equals(newStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                var snapshot = await _store.GetByIdIncludingInactiveAsync(sensorId, cancellationToken).ConfigureAwait(false);

                if (snapshot is null)
                {
                    _logger.LogWarning("SensorSnapshot not found for SensorId {SensorId}. Cannot reactivate.", sensorId);
                    return;
                }

                snapshot.Reactivate();
                await _store.UpdateAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Inactive, Maintenance, Faulty → deactivate
                await _store.DeleteAsync(sensorId, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
