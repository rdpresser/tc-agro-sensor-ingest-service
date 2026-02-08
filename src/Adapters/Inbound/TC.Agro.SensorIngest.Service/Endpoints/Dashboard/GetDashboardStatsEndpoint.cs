namespace TC.Agro.SensorIngest.Service.Endpoints.Dashboard
{
    public sealed class GetDashboardStatsEndpoint : BaseApiEndpoint<GetDashboardStatsQuery, DashboardStatsResponse>
    {
        public override void Configure()
        {
            Get("dashboard/stats");
            RoutePrefixOverride("sensors");
            PreProcessor<QueryCachingPreProcessorBehavior<GetDashboardStatsQuery, DashboardStatsResponse>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetDashboardStatsQuery, DashboardStatsResponse>>();

            Roles("Admin", "Producer");

            Description(
                x => x.Produces<DashboardStatsResponse>(200)
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets dashboard statistics.";
                s.Description = "Returns counts for properties, plots, sensors, and pending alerts. " +
                               "Properties and plots return 0 until farm-service integration.";
                s.Responses[200] = "Dashboard stats retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetDashboardStatsQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
