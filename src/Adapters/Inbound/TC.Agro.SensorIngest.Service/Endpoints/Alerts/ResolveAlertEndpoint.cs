namespace TC.Agro.SensorIngest.Service.Endpoints.Alerts
{
    public sealed class ResolveAlertEndpoint : BaseApiEndpoint<ResolveAlertCommand, ResolveAlertResponse>
    {
        public override void Configure()
        {
            Post("alerts/{AlertId}/resolve");
            RoutePrefixOverride("sensors");
            PostProcessor<LoggingCommandPostProcessorBehavior<ResolveAlertCommand, ResolveAlertResponse>>();
            PostProcessor<CacheInvalidationPostProcessorBehavior<ResolveAlertCommand, ResolveAlertResponse>>();

            Roles(AppRoles.Admin, AppRoles.Producer);

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

        public override async Task HandleAsync(ResolveAlertCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
