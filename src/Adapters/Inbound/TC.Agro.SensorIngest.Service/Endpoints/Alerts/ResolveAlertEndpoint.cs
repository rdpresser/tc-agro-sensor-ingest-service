using TC.Agro.SensorIngest.Application.UseCases.ResolveAlert;

namespace TC.Agro.SensorIngest.Service.Endpoints.Alerts
{
    public sealed class ResolveAlertEndpoint : Endpoint<ResolveAlertRequest, ResolveAlertResponse>
    {
        private readonly ResolveAlertCommandHandler _handler;

        public ResolveAlertEndpoint(ResolveAlertCommandHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override void Configure()
        {
            Post("alerts/{AlertId}/resolve");
            RoutePrefixOverride("sensors");

            Roles("Admin", "Producer");

            Description(
                x => x.Produces<ResolveAlertResponse>(200)
                      .ProducesProblemDetails()
                      .Produces(401)
                      .Produces(404));

            Summary(s =>
            {
                s.Summary = "Resolves an alert.";
                s.Description = "Marks an existing alert as resolved.";
                s.Responses[200] = "Alert resolved successfully.";
                s.Responses[400] = "Invalid request or alert already resolved.";
                s.Responses[401] = "Authentication required.";
                s.Responses[404] = "Alert not found.";
            });
        }

        public override async Task HandleAsync(ResolveAlertRequest req, CancellationToken ct)
        {
            var command = new ResolveAlertCommand(AlertId: req.AlertId);

            var response = await _handler.Handle(command, ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await Send.OkAsync(response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            if (response.IsNotFound())
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
        }
    }

    public sealed class ResolveAlertRequest
    {
        public Guid AlertId { get; set; }
    }
}
