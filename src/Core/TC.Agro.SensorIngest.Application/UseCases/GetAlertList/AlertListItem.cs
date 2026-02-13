namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    public sealed record AlertListItem(
        Guid Id,
        string Severity,
        string Title,
        string Message,
        Guid PlotId,
        string PlotName,
        Guid SensorId,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ResolvedAt);
}
