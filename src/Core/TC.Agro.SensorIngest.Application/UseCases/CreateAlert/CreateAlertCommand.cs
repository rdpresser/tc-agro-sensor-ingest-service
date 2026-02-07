namespace TC.Agro.SensorIngest.Application.UseCases.CreateAlert
{
    public sealed record CreateAlertCommand(
        string Severity,
        string Title,
        string Message,
        Guid PlotId,
        string PlotName,
        string SensorId) : IBaseCommand<CreateAlertResponse>;
}
