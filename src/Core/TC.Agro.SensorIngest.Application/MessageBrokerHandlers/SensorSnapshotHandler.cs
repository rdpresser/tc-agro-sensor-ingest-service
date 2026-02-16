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

        public SensorSnapshotHandler(ISensorSnapshotStore store, IUnitOfWork unitOfWork)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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
    }
}
