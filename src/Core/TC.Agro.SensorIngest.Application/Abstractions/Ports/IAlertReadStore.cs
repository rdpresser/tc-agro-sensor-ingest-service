using TC.Agro.SensorIngest.Application.UseCases.GetAlertList;

namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface IAlertReadStore
    {
        Task<IReadOnlyList<AlertListItem>> GetAlertsAsync(string? status, CancellationToken ct);
        Task<int> CountPendingAsync(CancellationToken ct);
    }
}
