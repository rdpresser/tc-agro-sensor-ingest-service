namespace TC.Agro.SensorIngest.Application.UseCases.CreateAlert
{
    public sealed record CreateAlertResponse(
        Guid Id,
        string Severity,
        string Status,
        DateTimeOffset CreatedAt,
        string Message = "Alert created successfully");
}
