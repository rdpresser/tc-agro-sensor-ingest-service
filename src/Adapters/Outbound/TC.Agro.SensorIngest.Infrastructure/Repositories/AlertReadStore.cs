using TC.Agro.SensorIngest.Application.UseCases.GetAlertList;
using TC.Agro.SensorIngest.Infrastructure.Extensions;

namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class AlertReadStore : IAlertReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public AlertReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<(IReadOnlyList<GetAlertListResponse>, int)> GetAlertsAsync(
            GetAlertListQuery query,
            CancellationToken ct = default)
        {
            var alertsQuery = _dbContext.Alerts
                .AsNoTracking();

            // Apply text filter
            alertsQuery = alertsQuery.ApplyTextFilter(query.Filter);

            // Apply status filter
            alertsQuery = alertsQuery.ApplyStatusFilter(query.Status);

            // Get total count before pagination
            var totalCount = await alertsQuery
                .CountAsync(ct)
                .ConfigureAwait(false);

            // Apply sorting, pagination, and projection
            var alerts = await alertsQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(x => new GetAlertListResponse(
                    x.Id,
                    x.Severity.Value,
                    x.Title,
                    x.Message,
                    x.PlotId,
                    x.PlotName,
                    x.SensorId,
                    x.Status.Value,
                    x.CreatedAt,
                    x.ResolvedAt))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (alerts, totalCount);
        }

        public async Task<int> CountPendingAsync(CancellationToken ct = default)
        {
            var pendingStatus = AlertStatus.CreatePending();
            return await _dbContext.Alerts
                .AsNoTracking()
                .CountAsync(x => x.Status == pendingStatus, ct)
                .ConfigureAwait(false);
        }
    }
}
