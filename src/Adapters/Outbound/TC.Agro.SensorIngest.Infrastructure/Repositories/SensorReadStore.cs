using TC.Agro.SensorIngest.Application.UseCases.GetSensorList;
using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SensorIngest.Infrastructure.Extensions;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class SensorReadStore : ISensorReadStore
    {
        private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan WarningThreshold = TimeSpan.FromMinutes(30);

        private readonly ApplicationDbContext _dbContext;

        public SensorReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<(IReadOnlyList<GetSensorListResponse>, int TotalCount)> GetSensorsAsync(
            GetSensorListQuery query,
            CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var baseQuery = _dbContext.SensorSnapshots
                .AsNoTracking()
                .Where(s => s.IsActive);

            baseQuery = baseQuery.ApplyPlotFilter(query.PlotId);
            baseQuery = baseQuery.ApplyTextFilter(query.Filter);

            var projected = baseQuery
                .Select(s => new SensorProjection
                {
                    Id = s.Id,
                    PlotId = s.PlotId,
                    PlotName = s.PlotName,
                    Label = s.Label,
                    CreatedAt = s.CreatedAt,
                    LastReadingTime = s.SensorReadings
                        .OrderByDescending(r => r.Time)
                        .Select(r => (DateTimeOffset?)r.Time)
                        .FirstOrDefault(),
                    LastTemperature = s.SensorReadings
                        .OrderByDescending(r => r.Time)
                        .Select(r => r.Temperature)
                        .FirstOrDefault(),
                    LastHumidity = s.SensorReadings
                        .OrderByDescending(r => r.Time)
                        .Select(r => r.Humidity)
                        .FirstOrDefault(),
                    LastSoilMoisture = s.SensorReadings
                        .OrderByDescending(r => r.Time)
                        .Select(r => r.SoilMoisture)
                        .FirstOrDefault(),
                    LastBatteryLevel = s.SensorReadings
                        .OrderByDescending(r => r.Time)
                        .Select(r => r.BatteryLevel)
                        .FirstOrDefault()
                });

            // Apply status filter on derived status
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                projected = query.Status.ToLowerInvariant() switch
                {
                    "online" => projected.Where(s =>
                        s.LastReadingTime != null && s.LastReadingTime > now.Add(-OnlineThreshold)),
                    "warning" => projected.Where(s =>
                        s.LastReadingTime != null &&
                        s.LastReadingTime <= now.Add(-OnlineThreshold) &&
                        s.LastReadingTime > now.Add(-WarningThreshold)),
                    "offline" => projected.Where(s =>
                        s.LastReadingTime == null || s.LastReadingTime <= now.Add(-WarningThreshold)),
                    _ => projected
                };
            }

            var totalCount = await projected
                .CountAsync(ct)
                .ConfigureAwait(false);

            var sorted = ApplySorting(projected, query.SortBy, query.SortDirection);

            var sensors = await sorted
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(s => new GetSensorListResponse(
                    s.Id,
                    s.Id,
                    s.PlotId,
                    s.PlotName,
                    s.LastReadingTime != null && s.LastReadingTime > now.Add(-OnlineThreshold) ? "Online"
                        : s.LastReadingTime != null && s.LastReadingTime > now.Add(-WarningThreshold) ? "Warning"
                        : "Offline",
                    s.LastBatteryLevel ?? 0,
                    s.LastReadingTime,
                    s.LastTemperature,
                    s.LastHumidity,
                    s.LastSoilMoisture))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (sensors, totalCount);
        }

        public async Task<SensorDetailItem?> GetByIdAsync(Guid sensorId, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var result = await _dbContext.SensorSnapshots
                .AsNoTracking()
                .Where(s => s.Id == sensorId && s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.PlotId,
                    s.PlotName,
                    s.CreatedAt,
                    LatestReading = s.SensorReadings
                        .OrderByDescending(r => r.Time)
                        .Select(r => new
                        {
                            r.Time,
                            r.Temperature,
                            r.Humidity,
                            r.SoilMoisture,
                            r.BatteryLevel
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (result is null)
                return null;

            var status = result.LatestReading != null && result.LatestReading.Time > now.Add(-OnlineThreshold) ? "Online"
                : result.LatestReading != null && result.LatestReading.Time > now.Add(-WarningThreshold) ? "Warning"
                : "Offline";

            return new SensorDetailItem(
                result.Id,
                result.Id,
                result.PlotId,
                result.PlotName,
                status,
                result.LatestReading?.BatteryLevel ?? 0,
                result.LatestReading?.Time,
                result.LatestReading?.Temperature,
                result.LatestReading?.Humidity,
                result.LatestReading?.SoilMoisture,
                result.CreatedAt);
        }

        public async Task<int> CountAsync(CancellationToken ct)
        {
            return await _dbContext.SensorSnapshots
                .AsNoTracking()
                .Where(s => s.IsActive)
                .CountAsync(ct)
                .ConfigureAwait(false);
        }

        private static IQueryable<SensorProjection> ApplySorting(
            IQueryable<SensorProjection> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(s => s.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "sensorid" => isAscending
                    ? query.OrderBy(s => s.Id)
                    : query.OrderByDescending(s => s.Id),
                "plotname" => isAscending
                    ? query.OrderBy(s => s.PlotName)
                    : query.OrderByDescending(s => s.PlotName),
                "battery" => isAscending
                    ? query.OrderBy(s => s.LastBatteryLevel)
                    : query.OrderByDescending(s => s.LastBatteryLevel),
                "lastreadingat" => isAscending
                    ? query.OrderBy(s => s.LastReadingTime)
                    : query.OrderByDescending(s => s.LastReadingTime),
                "createdat" => isAscending
                    ? query.OrderBy(s => s.CreatedAt)
                    : query.OrderByDescending(s => s.CreatedAt),
                _ => query.OrderByDescending(s => s.CreatedAt)
            };
        }

        /// <summary>
        /// Internal projection type for EF Core query translation with sorting support.
        /// </summary>
        private sealed class SensorProjection
        {
            public Guid Id { get; init; }
            public Guid PlotId { get; init; }
            public string PlotName { get; init; } = default!;
            public string? Label { get; init; }
            public DateTimeOffset CreatedAt { get; init; }
            public DateTimeOffset? LastReadingTime { get; init; }
            public double? LastTemperature { get; init; }
            public double? LastHumidity { get; init; }
            public double? LastSoilMoisture { get; init; }
            public double? LastBatteryLevel { get; init; }
        }
    }
}
