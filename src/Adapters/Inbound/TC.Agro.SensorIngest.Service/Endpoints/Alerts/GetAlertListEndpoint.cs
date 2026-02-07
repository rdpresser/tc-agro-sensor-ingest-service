using TC.Agro.SensorIngest.Application.UseCases.GetAlertList;

namespace TC.Agro.SensorIngest.Service.Endpoints.Alerts
{
    public sealed class GetAlertListEndpoint : Endpoint<GetAlertListRequest, GetAlertListResponse>
    {
        private readonly GetAlertListQueryHandler _handler;

        public GetAlertListEndpoint(GetAlertListQueryHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override void Configure()
        {
            Get("alerts");
            RoutePrefixOverride("sensors");

            Roles("Admin", "Producer");

            Description(
                x => x.Produces<GetAlertListResponse>(200)
                      .ProducesProblemDetails()
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets the list of alerts.";
                s.Description = "Retrieves all alerts, optionally filtered by status (Pending/Resolved).";
                s.Responses[200] = "Alert list retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetAlertListRequest req, CancellationToken ct)
        {
            var query = new GetAlertListQuery(Status: req.Status);

            var response = await _handler.Handle(query, ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await Send.OkAsync(response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
        }
    }

    public sealed class GetAlertListRequest
    {
        [QueryParam]
        public string? Status { get; set; }
    }
}
