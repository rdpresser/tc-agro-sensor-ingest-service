using TC.Agro.SensorIngest.Application.UseCases.CreateAlert;

namespace TC.Agro.SensorIngest.Service.Endpoints.Alerts
{
    public sealed class CreateAlertEndpoint : BaseApiEndpoint<CreateAlertCommand, CreateAlertResponse>
    {
        public override void Configure()
        {
            Post("alerts");
            RoutePrefixOverride("sensors");
            PostProcessor<LoggingCommandPostProcessorBehavior<CreateAlertCommand, CreateAlertResponse>>();

            Roles("Admin", "Sensor");

            Description(
                x => x.Produces<CreateAlertResponse>(201)
                      .ProducesProblemDetails()
                      .Produces(401)
                      .Produces(403));

            Summary(s =>
            {
                s.Summary = "Creates a new alert.";
                s.Description = "Creates a new alert for a sensor with specified severity level.";
                s.ExampleRequest = new CreateAlertCommand(
                    Severity: "Warning",
                    Title: "High Temperature",
                    Message: "Temperature exceeded 40C threshold",
                    PlotId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    PlotName: "Plot Alpha",
                    SensorId: "SENSOR-001");
                s.Responses[201] = "Alert created successfully.";
                s.Responses[400] = "Invalid request data.";
                s.Responses[401] = "Authentication required.";
                s.Responses[403] = "Insufficient permissions.";
            });
        }

        public override async Task HandleAsync(CreateAlertCommand req, CancellationToken ct)
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
