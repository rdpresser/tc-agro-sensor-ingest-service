namespace TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats
{
    public sealed record DashboardStatsResponse(
        int Properties,
        int Plots,
        int Sensors,
        int Alerts);
}
