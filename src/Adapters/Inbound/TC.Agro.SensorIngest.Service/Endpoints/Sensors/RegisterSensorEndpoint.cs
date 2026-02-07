using TC.Agro.SensorIngest.Application.UseCases.RegisterSensor;

namespace TC.Agro.SensorIngest.Service.Endpoints.Sensors
{
    public sealed class RegisterSensorEndpoint : BaseApiEndpoint<RegisterSensorCommand, RegisterSensorResponse>
    {
        public override void Configure()
        {
            Post("sensors");
            RoutePrefixOverride("sensors");
            PostProcessor<LoggingCommandPostProcessorBehavior<RegisterSensorCommand, RegisterSensorResponse>>();

            Roles("Admin", "Producer");

            Description(
                x => x.Produces<RegisterSensorResponse>(201)
                      .ProducesProblemDetails()
                      .Produces(401)
                      .Produces(403));

            Summary(s =>
            {
                s.Summary = "Registers a new sensor.";
                s.Description = "Creates a new sensor entity associated with a plot. " +
                               "The sensor starts with Online status.";
                s.ExampleRequest = new RegisterSensorCommand(
                    SensorId: "SENSOR-001",
                    PlotId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    PlotName: "Plot Alpha",
                    Battery: 95.0);
                s.Responses[201] = "Sensor registered successfully.";
                s.Responses[400] = "Invalid request data.";
                s.Responses[401] = "Authentication required.";
                s.Responses[403] = "Insufficient permissions.";
            });
        }

        public override async Task HandleAsync(RegisterSensorCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await HttpContext.Response.SendAsync(response.Value, 201, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
