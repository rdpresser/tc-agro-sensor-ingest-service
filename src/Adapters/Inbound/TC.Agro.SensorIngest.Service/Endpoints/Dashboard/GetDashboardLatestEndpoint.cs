using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;

namespace TC.Agro.SensorIngest.Service.Endpoints.Dashboard
{
    public sealed class GetDashboardLatestEndpoint : Endpoint<GetDashboardLatestRequest, GetLatestReadingsResponse>
    {
        private readonly GetLatestReadingsQueryHandler _handler;

        public GetDashboardLatestEndpoint(GetLatestReadingsQueryHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override void Configure()
        {
            Get("dashboard/latest");
            RoutePrefixOverride("sensors");

            Roles("Admin", "Producer");

            Description(
                x => x.Produces<GetLatestReadingsResponse>(200)
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets latest readings for dashboard.";
                s.Description = "Retrieves the most recent sensor readings for the dashboard overview.";
                s.Responses[200] = "Latest readings retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetDashboardLatestRequest req, CancellationToken ct)
        {
            var query = new GetLatestReadingsQuery(Limit: req.Limit ?? 5);

            var response = await _handler.Handle(query, ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await Send.OkAsync(response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
        }
    }

    public sealed class GetDashboardLatestRequest
    {
        [QueryParam]
        public int? Limit { get; set; }
    }
}
