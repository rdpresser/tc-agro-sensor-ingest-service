using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;
using TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorReadingReadStore : ISensorReadingReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public SensorReadingReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<(IReadOnlyList<GetLatestReadingsResponse> Readings, int TotalCount)> GetLatestReadingsAsync(
            GetLatestReadingsQuery query,
            CancellationToken cancellationToken = default)
        {
            var readingsQuery = _dbContext.SensorReadings.AsQueryable();

            if (query.SensorId.HasValue)
            {
                readingsQuery = readingsQuery.Where(x => x.SensorId == query.SensorId.Value);
            }

            if (query.PlotId.HasValue)
            {
                readingsQuery = readingsQuery.Where(x => x.Sensor.PlotId == query.PlotId.Value);
            }

            var totalCount = await readingsQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;
            var skip = (pageNumber - 1) * pageSize;

            var readings = await readingsQuery
                .OrderByDescending(x => x.Time)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new GetLatestReadingsResponse(
                    x.Id,
                    x.SensorId,
                    x.Sensor.PlotId,
                    x.Time,
                    x.Temperature,
                    x.Humidity,
                    x.SoilMoisture,
                    x.Rainfall,
                    x.BatteryLevel))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (readings, totalCount);
        }

        public async Task<(IReadOnlyList<GetReadingsHistoryResponse> Readings, int TotalCount)> GetHistoryAsync(
            GetReadingsHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            var to = DateTimeOffset.UtcNow;
            var from = to.AddDays(-Math.Clamp(query.Days, 1, 30));

            var historyQuery = _dbContext.SensorReadings
                .Where(x => x.SensorId == query.SensorId && x.Time >= from && x.Time <= to);

            var totalCount = await historyQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;
            var skip = (pageNumber - 1) * pageSize;

            var readings = await historyQuery
                .OrderByDescending(x => x.Time)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new GetReadingsHistoryResponse(
                    x.Id,
                    x.SensorId,
                    x.Sensor.PlotId,
                    x.Time,
                    x.Temperature,
                    x.Humidity,
                    x.SoilMoisture,
                    x.Rainfall,
                    x.BatteryLevel))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (readings, totalCount);
        }

        public async Task<IReadOnlyList<HourlyAggregateItem>> GetHourlyAggregatesAsync(
            string sensorId,
            int days = 7,
            CancellationToken cancellationToken = default)
        {
            var clampedDays = Math.Clamp(days, 1, 90);

            var sql = $@"
                SELECT
                    time_bucket('1 hour', time) AS hour,
                    AVG(temperature) AS avg_temperature,
                    MAX(temperature) AS max_temperature,
                    MIN(temperature) AS min_temperature,
                    AVG(humidity) AS avg_humidity,
                    AVG(soil_moisture) AS avg_soil_moisture
                FROM sensor_readings
                WHERE sensor_id = @sensorId
                  AND time > now() - interval '{clampedDays} days'
                GROUP BY hour
                ORDER BY hour DESC";

            return await _dbContext.Database
                .SqlQueryRaw<HourlyAggregateItem>(sql, new Npgsql.NpgsqlParameter("@sensorId", sensorId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
