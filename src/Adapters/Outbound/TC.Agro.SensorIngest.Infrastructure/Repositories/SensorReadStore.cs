using TC.Agro.SensorIngest.Application.UseCases.GetSensorList;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorReadStore : ISensorReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public SensorReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IReadOnlyList<SensorListItem>> GetSensorsAsync(Guid? plotId, CancellationToken ct)
        {
            var query = _dbContext.Sensors
                .AsNoTracking()
                .AsQueryable();

            if (plotId.HasValue)
                query = query.Where(x => x.PlotId == plotId.Value);

            var entities = await query
                .OrderBy(x => x.SensorId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return entities.Select(x => new SensorListItem(
                x.Id,
                x.SensorId,
                x.PlotId,
                x.PlotName,
                x.Status.Value,
                x.Battery,
                x.LastReadingAt,
                x.LastTemperature,
                x.LastHumidity,
                x.LastSoilMoisture)).ToList();
        }

        public async Task<SensorDetailItem?> GetByIdAsync(Guid sensorId, CancellationToken ct)
        {
            var entity = await _dbContext.Sensors
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SensorId == sensorId, ct)
                .ConfigureAwait(false);

            if (entity is null)
                return null;

            return new SensorDetailItem(
                entity.Id,
                entity.SensorId,
                entity.PlotId,
                entity.PlotName,
                entity.Status.Value,
                entity.Battery,
                entity.LastReadingAt,
                entity.LastTemperature,
                entity.LastHumidity,
                entity.LastSoilMoisture,
                entity.CreatedAt);
        }

        public async Task<int> CountAsync(CancellationToken ct)
        {
            return await _dbContext.Sensors
                .AsNoTracking()
                .CountAsync(ct)
                .ConfigureAwait(false);
        }
    }
}
