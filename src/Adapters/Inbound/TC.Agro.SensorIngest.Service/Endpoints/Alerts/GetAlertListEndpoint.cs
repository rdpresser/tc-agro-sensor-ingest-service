namespace TC.Agro.SensorIngest.Service.Endpoints.Alerts
{
    public sealed class GetAlertListEndpoint : BaseApiEndpoint<GetAlertListQuery, GetAlertListResponse>
    {
        public override void Configure()
        {
            Get("alerts");
            RoutePrefixOverride("sensors");
            RequestBinder(new RequestBinder<GetAlertListQuery>(BindingSource.QueryParams));
            PreProcessor<QueryCachingPreProcessorBehavior<GetAlertListQuery, GetAlertListResponse>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetAlertListQuery, GetAlertListResponse>>();

            Roles(AppRoles.Admin, AppRoles.Producer);

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

        public override async Task HandleAsync(GetAlertListQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
