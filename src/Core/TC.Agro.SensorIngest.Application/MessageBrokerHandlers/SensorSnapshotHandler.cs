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
        private const string DefaultSensorLabel = "Unnamed Sensor";

        private static string NormalizeLabel(string? label) =>
            string.IsNullOrWhiteSpace(label) ? DefaultSensorLabel : label;

        private static SensorSnapshot CreateSnapshot(SensorRegisteredIntegrationEvent data, string label) =>
            SensorSnapshot.Create(
                data.SensorId,
                data.OwnerId,
                data.PropertyId,
                data.PlotId,
                data.OwnerId,
                label,
                plotName: data.PlotName,
                propertyName: data.PropertyName,
                createdAt: data.OccurredOn);

        private static SensorSnapshot CreateSnapshot(SensorOperationalStatusChangedIntegrationEvent data, string label) =>
            SensorSnapshot.Create(
                data.SensorId,
                data.OwnerId,
                data.PropertyId,
                data.PlotId,
                data.OwnerId,
                label,
                plotName: data.PlotName,
                propertyName: data.PropertyName,
                createdAt: data.OccurredOn);

        private static void UpdateSnapshot(SensorSnapshot snapshot, SensorOperationalStatusChangedIntegrationEvent data, string label) =>
            snapshot.Update(
                data.OwnerId,
                data.PropertyId,
                data.PlotId,
                data.OwnerId,
                label,
                plotName: data.PlotName,
                propertyName: data.PropertyName,
                status: data.Status);

        public SensorSnapshotHandler(ISensorSnapshotStore store, IUnitOfWork unitOfWork)
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

            var data = @event.EventData;
            var label = NormalizeLabel(data.Label);
            var snapshot = CreateSnapshot(data, label);

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

        // -------------------------
        // Sensor Operational Status Changed
        // -------------------------
        /// <summary>
        /// Handles the SensorOperationalStatusChangedIntegrationEvent by updating
        /// all fields in the corresponding SensorSnapshot (OwnerId, PropertyId, PlotId, Label, PlotName, and PropertyName).
        /// If the snapshot doesn't exist, creates it defensively.
        /// </summary>
        public async Task HandleAsync(
            EventContext<SensorOperationalStatusChangedIntegrationEvent> @event,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);
            
            var data = @event.EventData;
            var snapshot = await _store.GetByIdAsync(
                data.SensorId,
                cancellationToken).ConfigureAwait(false);

            var label = NormalizeLabel(data.Label);

            if (snapshot is null)
            {
                snapshot = CreateSnapshot(data, label);

                await _store.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                UpdateSnapshot(snapshot, data, label);

                await _store.UpdateAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // -------------------------
        // Sensor Deactivated
        // -------------------------
        /// <summary>
        /// Handles the SensorDeactivatedIntegrationEvent by performing a soft delete
        /// on the corresponding SensorSnapshot, setting IsActive to false.
        /// If the snapshot doesn't exist, does nothing.
        /// </summary>
        public async Task HandleAsync(
            EventContext<SensorDeactivatedIntegrationEvent> @event,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var snapshot = await _store.GetByIdAsync(
                @event.EventData.SensorId,
                cancellationToken).ConfigureAwait(false);

            if (snapshot is null)
                return;

            snapshot.Delete();
            await _store.UpdateAsync(snapshot, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

    }
}
