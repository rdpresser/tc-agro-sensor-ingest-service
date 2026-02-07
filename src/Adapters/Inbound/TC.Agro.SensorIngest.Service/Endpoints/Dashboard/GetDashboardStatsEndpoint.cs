using TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats;

namespace TC.Agro.SensorIngest.Service.Endpoints.Dashboard
{
    public sealed class GetDashboardStatsEndpoint : EndpointWithoutRequest<DashboardStatsResponse>
    {
        private readonly GetDashboardStatsQueryHandler _handler;

        public GetDashboardStatsEndpoint(GetDashboardStatsQueryHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override void Configure()
        {
            Get("dashboard/stats");
            RoutePrefixOverride("sensors");

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

        public override async Task HandleAsync(CancellationToken ct)
        {
            var query = new GetDashboardStatsQuery();
            var response = await _handler.Handle(query, ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await SendOkAsync(response.Value, ct).ConfigureAwait(false);
                return;
            }

            await SendErrorsAsync((int)HttpStatusCode.InternalServerError, ct).ConfigureAwait(false);
        }
    }
}
