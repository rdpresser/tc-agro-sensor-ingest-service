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

        /// <summary>
        /// Updates an existing sensor snapshot in the store.
        /// </summary>
        /// <param name="snapshot">The sensor snapshot to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateAsync(SensorSnapshot snapshot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a sensor snapshot from the store by marking it as inactive.
        /// </summary>
        /// <param name="id">The sensor snapshot identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing sensor snapshot in the store.
        /// </summary>
        /// <param name="snapshot">The sensor snapshot to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateAsync(SensorSnapshot snapshot, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// Retrieves active sensor snapshots for the given identifiers in a single query.
        /// Used for batch validation to avoid N+1 queries.
        /// </summary>
        /// <param name="ids">The sensor identifiers</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary mapping sensor ID to its snapshot (only active sensors)</returns>
        Task<IReadOnlyDictionary<Guid, SensorSnapshot>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all active sensor snapshots.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A read-only list of active sensor snapshots</returns>
        Task<IReadOnlyList<SensorSnapshot>> GetAllActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a sensor snapshot by its identifier, bypassing the global IsActive query filter.
        /// Used for reactivation scenarios where the snapshot may be inactive.
        /// </summary>
        /// <param name="id">The sensor snapshot identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The sensor snapshot (active or inactive), or null if not found</returns>
        Task<SensorSnapshot?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
