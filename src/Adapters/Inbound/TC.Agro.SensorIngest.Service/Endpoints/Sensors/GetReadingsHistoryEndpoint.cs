using TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory;

namespace TC.Agro.SensorIngest.Service.Endpoints.Sensors
{
    public sealed class GetReadingsHistoryEndpoint : Endpoint<GetReadingsHistoryRequest, GetReadingsHistoryResponse>
    {
        private readonly GetReadingsHistoryQueryHandler _handler;

        public GetReadingsHistoryEndpoint(GetReadingsHistoryQueryHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override void Configure()
        {
            Get("{SensorId}/readings");
            RoutePrefixOverride("sensors");

            Roles("Admin", "Producer");

            Description(
                x => x.Produces<GetReadingsHistoryResponse>(200)
                      .ProducesProblemDetails()
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets reading history for a sensor.";
                s.Description = "Retrieves the reading history for a specific sensor over the given number of days.";
                s.Responses[200] = "Reading history retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetReadingsHistoryRequest req, CancellationToken ct)
        {
            var query = new GetReadingsHistoryQuery(
                SensorId: req.SensorId,
                Days: req.Days ?? 7);

            var response = await _handler.Handle(query, ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await Send.OkAsync(response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
        }
    }

    public sealed class GetReadingsHistoryRequest
    {
        public string SensorId { get; set; } = default!;

        [QueryParam]
        public int? Days { get; set; }
    }
}
