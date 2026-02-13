namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorAggregateRepository
        : BaseRepository<SensorAggregate, ApplicationDbContext>, ISensorAggregateRepository
    {
        public SensorAggregateRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<SensorAggregate?> GetBySensorIdAsync(Guid sensorId, CancellationToken ct = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(x => x.SensorId == sensorId, ct)
                .ConfigureAwait(false);
        }

        public async Task<bool> SensorIdExistsAsync(Guid sensorId, CancellationToken ct = default)
        {
            return await DbSet
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(x => x.SensorId == sensorId, ct)
                .ConfigureAwait(false);
        }
    }
}
