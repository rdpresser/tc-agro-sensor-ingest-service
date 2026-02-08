namespace TC.Agro.SensorIngest.Service.Hubs
{
    public sealed record AlertHubDto(
        Guid Id,
        string Severity,
        string Title,
        string Message,
        Guid PlotId,
        string PlotName,
        string SensorId,
        string Status,
        DateTimeOffset CreatedAt);
}
