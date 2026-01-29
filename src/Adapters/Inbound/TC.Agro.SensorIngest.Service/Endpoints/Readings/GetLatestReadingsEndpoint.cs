namespace TC.Agro.SensorIngest.Service.Endpoints.Readings
{
    public sealed class GetLatestReadingsEndpoint : Endpoint<GetLatestReadingsRequest, GetLatestReadingsResponse>
    {
        private readonly GetLatestReadingsQueryHandler _handler;

        public GetLatestReadingsEndpoint(GetLatestReadingsQueryHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override void Configure()
        {
            Get("readings/latest");
            RoutePrefixOverride("sensors");

            // JWT Authentication required
            Roles("Admin", "Producer");

            Description(
                x => x.Produces<GetLatestReadingsResponse>(200)
                      .ProducesProblemDetails()
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets the latest sensor readings.";
                s.Description = "Retrieves the most recent sensor readings, optionally filtered by sensor ID or plot ID. " +
                               "Results are cached for 60 seconds.";
                s.Responses[200] = "Latest readings retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetLatestReadingsRequest req, CancellationToken ct)
        {
            var query = new GetLatestReadingsQuery(
                SensorId: req.SensorId,
                PlotId: req.PlotId,
                Limit: req.Limit ?? 10);

            var response = await _handler.Handle(query, ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await Send.OkAsync(response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
        }
    }

    public sealed class GetLatestReadingsRequest
    {
        [QueryParam]
        public string? SensorId { get; set; }

        [QueryParam]
        public Guid? PlotId { get; set; }

        [QueryParam]
        public int? Limit { get; set; }
    }
}
