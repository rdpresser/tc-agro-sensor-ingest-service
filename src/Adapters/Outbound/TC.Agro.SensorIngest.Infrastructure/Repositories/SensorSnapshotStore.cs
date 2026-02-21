using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    internal class SensorSnapshotStore : ISensorSnapshotStore
    {
        private readonly ApplicationDbContext _dbContext;

        public SensorSnapshotStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public async Task AddAsync(SensorSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            await _dbContext.SensorSnapshots.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(SensorSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            var existingSnapshot = await _dbContext.SensorSnapshots
                .FirstOrDefaultAsync(o => o.Id == snapshot.Id, cancellationToken)
                .ConfigureAwait(false);

            if (existingSnapshot == null)
                return;

            _dbContext.SensorSnapshots.Update(snapshot);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var snapshot = await _dbContext.SensorSnapshots
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (snapshot == null)
                return;

            snapshot.Delete();
            _dbContext.SensorSnapshots.Update(snapshot);
        }

        /// <inheritdoc />
        public async Task<SensorSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.SensorSnapshots
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.SensorSnapshots
                .AsNoTracking()
                .AnyAsync(s => s.Id == id && s.IsActive, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<Guid, SensorSnapshot>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();

            return await _dbContext.SensorSnapshots
                .AsNoTracking()
                .Where(s => idList.Contains(s.Id) && s.IsActive)
                .ToDictionaryAsync(s => s.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
