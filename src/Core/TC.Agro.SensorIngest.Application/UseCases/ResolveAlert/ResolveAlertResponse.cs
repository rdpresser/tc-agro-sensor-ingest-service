namespace TC.Agro.SensorIngest.Application.UseCases.ResolveAlert
{
    public sealed record ResolveAlertResponse(
        Guid Id,
        string Status,
        DateTimeOffset? ResolvedAt,
        string Message = "Alert resolved successfully");
}
