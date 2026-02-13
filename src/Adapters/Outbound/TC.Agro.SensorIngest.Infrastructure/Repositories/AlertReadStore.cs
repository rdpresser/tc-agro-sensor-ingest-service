using TC.Agro.SensorIngest.Application.UseCases.GetAlertList;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class AlertReadStore : IAlertReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public AlertReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IReadOnlyList<AlertListItem>> GetAlertsAsync(string? status, CancellationToken ct)
        {
            var query = _dbContext.Alerts
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusResult = AlertStatus.Create(status);
                if (!statusResult.IsSuccess)
                    throw new ArgumentException($"Invalid alert status filter '{status}'. Valid values: Pending, Resolved.", nameof(status));

                query = query.Where(x => x.Status == statusResult.Value);
            }

            var entities = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return entities.Select(x => new AlertListItem(
                x.Id,
                x.Severity.Value,
                x.Title,
                x.Message,
                x.PlotId,
                x.PlotName,
                x.SensorId,
                x.Status.Value,
                x.CreatedAt,
                x.ResolvedAt)).ToList();
        }

        public async Task<int> CountPendingAsync(CancellationToken ct)
        {
            var pendingStatus = AlertStatus.CreatePending();
            return await _dbContext.Alerts
                .AsNoTracking()
                .CountAsync(x => x.Status == pendingStatus, ct)
                .ConfigureAwait(false);
        }
    }
}
