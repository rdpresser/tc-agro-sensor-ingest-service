namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorAggregateRepository
        : BaseRepository<SensorAggregate, ApplicationDbContext>, ISensorAggregateRepository
    {
        public SensorAggregateRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<SensorAggregate?> GetBySensorIdAsync(string sensorId, CancellationToken ct = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(x => x.SensorId == sensorId && x.IsActive, ct)
                .ConfigureAwait(false);
        }

        public async Task<bool> SensorIdExistsAsync(string sensorId, CancellationToken ct = default)
        {
            return await DbSet
                .AsNoTracking()
                .AnyAsync(x => x.SensorId == sensorId, ct)
                .ConfigureAwait(false);
        }
    }
}
