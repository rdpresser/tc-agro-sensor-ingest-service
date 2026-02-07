namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface IAlertReadStore
    {
        Task<IReadOnlyList<AlertListDto>> GetAlertsAsync(string? status, CancellationToken ct);
        Task<int> CountPendingAsync(CancellationToken ct);
    }

    public sealed record AlertListDto(
        Guid Id,
        string Severity,
        string Title,
        string Message,
        Guid PlotId,
        string PlotName,
        string SensorId,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ResolvedAt);
}
