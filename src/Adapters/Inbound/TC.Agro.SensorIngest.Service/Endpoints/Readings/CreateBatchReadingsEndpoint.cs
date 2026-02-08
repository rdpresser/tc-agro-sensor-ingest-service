namespace TC.Agro.SensorIngest.Service.Endpoints.Readings
{
    public sealed class CreateBatchReadingsEndpoint : BaseApiEndpoint<CreateBatchReadingsCommand, CreateBatchReadingsResponse>
    {
        public override void Configure()
        {
            Post("batch");
            RoutePrefixOverride("sensors");
            PostProcessor<LoggingCommandPostProcessorBehavior<CreateBatchReadingsCommand, CreateBatchReadingsResponse>>();
            PostProcessor<CacheInvalidationPostProcessorBehavior<CreateBatchReadingsCommand, CreateBatchReadingsResponse>>();

            Roles(AppRoles.Admin, AppRoles.Producer, AppRoles.Sensor);

            Description(
                x => x.Produces<CreateBatchReadingsResponse>(202)
                      .ProducesProblemDetails()
                      .Produces(401)
                      .Produces(403));

            Summary(s =>
            {
                s.Summary = "Ingests multiple sensor readings in batch.";
                s.Description = "Receives an array of sensor readings for bulk processing. " +
                               "Each reading is validated individually. Maximum batch size is 1000 readings.";
                s.ExampleRequest = new CreateBatchReadingsCommand(
                [
                    new SensorReadingInput(
                        SensorId: "sensor-001",
                        PlotId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                        Timestamp: DateTime.UtcNow,
                        Temperature: 28.5,
                        Humidity: 65.2,
                        SoilMoisture: 42.1,
                        Rainfall: 0.0,
                        BatteryLevel: 85.0),
                    new SensorReadingInput(
                        SensorId: "sensor-002",
                        PlotId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                        Timestamp: DateTime.UtcNow,
                        Temperature: 29.1,
                        Humidity: 62.8,
                        SoilMoisture: 38.5,
                        Rainfall: 0.0,
                        BatteryLevel: 92.0)
                ]);
                s.ResponseExamples[202] = new CreateBatchReadingsResponse(
                    ProcessedCount: 2,
                    FailedCount: 0,
                    Results:
                    [
                        new BatchReadingResult(Guid.NewGuid(), "sensor-001", true),
                        new BatchReadingResult(Guid.NewGuid(), "sensor-002", true)
                    ]);
                s.Responses[202] = "Batch accepted and processed.";
                s.Responses[400] = "Invalid request data.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(CreateBatchReadingsCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await SendAsync(response.Value, 202, ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
