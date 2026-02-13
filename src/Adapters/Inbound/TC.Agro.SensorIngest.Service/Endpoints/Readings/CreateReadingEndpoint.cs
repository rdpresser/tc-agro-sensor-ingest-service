namespace TC.Agro.SensorIngest.Service.Endpoints.Readings
{
    public sealed class CreateReadingEndpoint : BaseApiEndpoint<CreateReadingCommand, CreateReadingResponse>
    {
        public override void Configure()
        {
            Post("readings");
            PostProcessor<LoggingCommandPostProcessorBehavior<CreateReadingCommand, CreateReadingResponse>>();
            PostProcessor<CacheInvalidationPostProcessorBehavior<CreateReadingCommand, CreateReadingResponse>>();

            Roles(AppRoles.Admin, AppRoles.Producer, AppRoles.Sensor);

            Description(
                x => x.Produces<CreateReadingResponse>(202)
                      .ProducesProblemDetails()
                      .Produces(401)
                      .Produces(403));

            Summary(s =>
            {
                s.Summary = "Ingests a single sensor reading.";
                s.Description = "Receives sensor data (temperature, humidity, soil moisture, rainfall) for a specific plot. " +
                               "The reading is persisted to TimescaleDB and an event is published for analytics processing.";
                s.ExampleRequest = new CreateReadingCommand(
                    SensorId: Guid.NewGuid(),
                    PlotId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    Timestamp: DateTime.UtcNow,
                    Temperature: 28.5,
                    Humidity: 65.2,
                    SoilMoisture: 42.1,
                    Rainfall: 0.0,
                    BatteryLevel: 85.0);
                s.ResponseExamples[202] = new CreateReadingResponse(
                    ReadingId: Guid.NewGuid(),
                    SensorId: Guid.NewGuid(),
                    PlotId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    Timestamp: DateTime.UtcNow);
                s.Responses[202] = "Reading accepted and queued for processing.";
                s.Responses[400] = "Invalid request data.";
                s.Responses[401] = "Authentication required.";
                s.Responses[403] = "Insufficient permissions.";
            });
        }

        public override async Task HandleAsync(CreateReadingCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
