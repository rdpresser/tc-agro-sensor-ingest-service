namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    public sealed record GetAlertListResponse(IReadOnlyList<AlertListItem> Alerts);
}
