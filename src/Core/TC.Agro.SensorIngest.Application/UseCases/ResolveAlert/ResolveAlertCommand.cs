namespace TC.Agro.SensorIngest.Application.UseCases.ResolveAlert
{
    public sealed record ResolveAlertCommand(
        Guid AlertId) : IBaseCommand<ResolveAlertResponse>;
}
