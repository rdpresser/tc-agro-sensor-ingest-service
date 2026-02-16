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

        public async Task<IEnumerable<LatestReadingItem>> GetLatestReadingsAsync(
            Guid? sensorId = null,
            Guid? plotId = null,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.SensorReadings.AsQueryable();

            if (sensorId.HasValue)
                query = query.Where(x => x.SensorId == sensorId.Value);

            if (plotId.HasValue)
                query = query.Where(x => x.PlotId == plotId.Value);

            return await query
                .OrderByDescending(x => x.Time)
                .Take(limit)
                .Select(x => new LatestReadingItem(
                    x.Id,
                    x.SensorId,
                    x.PlotId,
                    x.Time.Date,
                    x.Temperature,
                    x.Humidity,
                    x.SoilMoisture,
                    x.Rainfall,
                    x.BatteryLevel))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReadingHistoryItem>> GetHistoryAsync(
            Guid sensorId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.SensorReadings
                .Where(x => x.SensorId == sensorId && x.Time >= from && x.Time <= to)
                .OrderByDescending(x => x.Time)
                .Select(x => new ReadingHistoryItem(
                    x.Id,
                    x.SensorId,
                    x.PlotId,
                    x.Time.Date,
                    x.Temperature,
                    x.Humidity,
                    x.SoilMoisture,
                    x.Rainfall,
                    x.BatteryLevel))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<HourlyAggregateItem>> GetHourlyAggregatesAsync(
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
