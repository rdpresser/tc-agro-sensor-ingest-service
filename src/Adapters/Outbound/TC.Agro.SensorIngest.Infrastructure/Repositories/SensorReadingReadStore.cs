using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;
using TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorReadingReadStore : ISensorReadingReadStore
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserContext _userContext;

        public SensorReadingReadStore(ApplicationDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<SensorReadingAggregate> FilteredDbSet => _userContext.IsAdmin
            ? _dbContext.SensorReadings
            : _dbContext.SensorReadings.Where(x => x.Sensor.OwnerId == _userContext.Id);

        public async Task<(IReadOnlyList<GetLatestReadingsResponse> Readings, int TotalCount)> GetLatestReadingsAsync(
            GetLatestReadingsQuery query,
            CancellationToken cancellationToken = default)
        {
            var readingsQuery = FilteredDbSet
                .AsNoTracking();

            if (_userContext.IsAdmin && query.OwnerId is not null && query.OwnerId.HasValue && query.OwnerId.Value != Guid.Empty)
            {
                //when loggedin as admin on frontend
                readingsQuery = readingsQuery.Where(x => x.Sensor.OwnerId == query.OwnerId);
            }

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
                    x.Sensor.PlotId,
                    x.SensorId,
                    x.Sensor.Label,
                    x.Sensor.PlotName,
                    x.Sensor.PropertyName,
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

            var historyQuery = FilteredDbSet
                .AsNoTracking()
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
                    x.Sensor.PlotName,
                    x.Sensor.PropertyName,
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
