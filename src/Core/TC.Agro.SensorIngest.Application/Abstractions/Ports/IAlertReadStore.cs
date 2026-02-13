namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface IAlertReadStore
    {
        Task<(IReadOnlyList<GetAlertListResponse>, int TotalCount)> GetAlertsAsync(
            GetAlertListQuery query,
            CancellationToken ct = default);

        Task<int> CountPendingAsync(CancellationToken ct = default);
    }
}
