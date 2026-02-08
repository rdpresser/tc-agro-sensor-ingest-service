namespace TC.Agro.SensorIngest.Service.Endpoints.Readings
{
    public sealed class GetLatestReadingsEndpoint : BaseApiEndpoint<GetLatestReadingsQuery, GetLatestReadingsResponse>
    {
        public override void Configure()
        {
            Get("readings/latest");
            RoutePrefixOverride("sensors");
            RequestBinder(new RequestBinder<GetLatestReadingsQuery>(BindingSource.QueryParams));
            PreProcessor<QueryCachingPreProcessorBehavior<GetLatestReadingsQuery, GetLatestReadingsResponse>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetLatestReadingsQuery, GetLatestReadingsResponse>>();

            Roles(AppRoles.Admin, AppRoles.Producer);

            Description(
                x => x.Produces<GetLatestReadingsResponse>(200)
                      .ProducesProblemDetails()
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets the latest sensor readings.";
                s.Description = "Retrieves the most recent sensor readings, optionally filtered by sensor ID or plot ID.";
                s.Responses[200] = "Latest readings retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetLatestReadingsQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
