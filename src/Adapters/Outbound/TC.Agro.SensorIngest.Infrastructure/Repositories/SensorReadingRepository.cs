namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorReadingRepository
        : BaseRepository<SensorReadingAggregate, ApplicationDbContext>, ISensorReadingRepository
    {
        public SensorReadingRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<SensorReadingAggregate>> GetLatestBySensorIdAsync(
            Guid sensorId,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(x => x.SensorId == sensorId)
                .OrderByDescending(x => x.Time)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<SensorReadingAggregate>> GetByPlotIdAsync(
            Guid plotId,
            DateTime? from = null,
            DateTime? to = null,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet.Where(x => x.Sensor.PlotId == plotId);

            if (from.HasValue)
                query = query.Where(x => x.Time >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.Time <= to.Value);

            return await query
                .OrderByDescending(x => x.Time)
                .Take(limit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task AddRangeAsync(
            IEnumerable<SensorReadingAggregate> readings,
            CancellationToken cancellationToken = default)
        {
            await DbSet.AddRangeAsync(readings, cancellationToken).ConfigureAwait(false);
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
