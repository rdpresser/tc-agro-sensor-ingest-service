namespace TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats
{
    public sealed record GetDashboardStatsResponse(
        int Properties,
        int Plots,
        int Sensors,
        int Alerts);
}
