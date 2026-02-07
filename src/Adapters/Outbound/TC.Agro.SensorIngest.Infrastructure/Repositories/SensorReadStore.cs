using TC.Agro.SensorIngest.Application.Abstractions.Ports;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorReadStore : ISensorReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public SensorReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IReadOnlyList<SensorListDto>> GetSensorsAsync(Guid? plotId, CancellationToken ct)
        {
            var query = _dbContext.Sensors
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (plotId.HasValue)
                query = query.Where(x => x.PlotId == plotId.Value);

            var entities = await query
                .OrderBy(x => x.SensorId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return entities.Select(x => new SensorListDto(
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

        public async Task<SensorDetailDto?> GetByIdAsync(string sensorId, CancellationToken ct)
        {
            var entity = await _dbContext.Sensors
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SensorId == sensorId && x.IsActive, ct)
                .ConfigureAwait(false);

            if (entity is null)
                return null;

            return new SensorDetailDto(
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
                .CountAsync(x => x.IsActive, ct)
                .ConfigureAwait(false);
        }
    }
}
