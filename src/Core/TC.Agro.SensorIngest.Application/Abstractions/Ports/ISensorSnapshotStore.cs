using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    /// <summary>
    /// Port for read-optimized sensor snapshot store operations.
    /// Handles persistence of sensor snapshots projected from external integration events.
    /// </summary>
    public interface ISensorSnapshotStore
    {
        /// <summary>
        /// Adds a new sensor snapshot to the store.
        /// </summary>
        /// <param name="snapshot">The sensor snapshot to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task AddAsync(SensorSnapshot snapshot, CancellationToken cancellationToken = default);

        ///// <summary>
        ///// Updates an existing sensor snapshot in the store.
        ///// </summary>
        ///// <param name="snapshot">The sensor snapshot to update</param>
        ///// <param name="cancellationToken">Cancellation token</param>
        ////Task UpdateAsync(SensorSnapshot snapshot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a sensor snapshot from the store by marking it as inactive.
        /// </summary>
        /// <param name="id">The sensor snapshot identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a sensor snapshot by its identifier.
        /// </summary>
        /// <param name="id">The sensor snapshot identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The sensor snapshot, or null if not found</returns>
        Task<SensorSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether an active sensor snapshot exists for the given identifier.
        /// Used to validate sensor existence before accepting readings.
        /// </summary>
        /// <param name="id">The sensor identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if an active sensor snapshot exists</returns>
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
