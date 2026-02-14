using TC.Agro.SensorIngest.Application.UseCases.GetSensorList;
using TC.Agro.SensorIngest.Infrastructure.Extensions;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorReadStore : ISensorReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public SensorReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<(IReadOnlyList<GetSensorListResponse>, int TotalCount)> GetSensorsAsync(
            GetSensorListQuery query,
            CancellationToken ct)
        {
            var sensorsQuery = _dbContext.Sensors
                .AsNoTracking()
                .AsQueryable();

            sensorsQuery = sensorsQuery.ApplyPlotFilter(query.PlotId);
            sensorsQuery = sensorsQuery.ApplyTextFilter(query.Filter);
            sensorsQuery = sensorsQuery.ApplyStatusFilter(query.Status);

            var totalCount = await sensorsQuery
                .CountAsync(ct)
                .ConfigureAwait(false);

            var sensors = await sensorsQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(x => new GetSensorListResponse(
                    x.Id,
                    x.SensorId,
                    x.PlotId,
                    x.PlotName,
                    x.Status.Value,
                    x.Battery,
                    x.LastReadingAt,
                    x.LastTemperature,
                    x.LastHumidity,
                    x.LastSoilMoisture))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (sensors, totalCount);
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
