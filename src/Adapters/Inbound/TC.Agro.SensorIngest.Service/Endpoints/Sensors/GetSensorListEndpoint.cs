using TC.Agro.SensorIngest.Application.UseCases.GetSensorList;

namespace TC.Agro.SensorIngest.Service.Endpoints.Sensors
{
    public sealed class GetSensorListEndpoint : Endpoint<GetSensorListRequest, GetSensorListResponse>
    {
        private readonly GetSensorListQueryHandler _handler;

        public GetSensorListEndpoint(GetSensorListQueryHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override void Configure()
        {
            Get("");
            RoutePrefixOverride("sensors");

            Roles("Admin", "Producer");

            Description(
                x => x.Produces<GetSensorListResponse>(200)
                      .ProducesProblemDetails()
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets the list of registered sensors.";
                s.Description = "Retrieves all registered sensors, optionally filtered by plot ID.";
                s.Responses[200] = "Sensor list retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetSensorListRequest req, CancellationToken ct)
        {
            var query = new GetSensorListQuery(PlotId: req.PlotId);

            var response = await _handler.Handle(query, ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await Send.OkAsync(response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
        }
    }

    public sealed class GetSensorListRequest
    {
        [QueryParam]
        public Guid? PlotId { get; set; }
    }
}
