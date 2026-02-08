namespace TC.Agro.SensorIngest.Service.Endpoints.Sensors
{
    public sealed class GetReadingsHistoryEndpoint : BaseApiEndpoint<GetReadingsHistoryQuery, GetReadingsHistoryResponse>
    {
        public override void Configure()
        {
            Get("{SensorId}/readings");
            RoutePrefixOverride("sensors");
            PreProcessor<QueryCachingPreProcessorBehavior<GetReadingsHistoryQuery, GetReadingsHistoryResponse>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetReadingsHistoryQuery, GetReadingsHistoryResponse>>();

            Roles(AppRoles.Admin, AppRoles.Producer);

            Description(
                x => x.Produces<GetReadingsHistoryResponse>(200)
                      .ProducesProblemDetails()
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets reading history for a sensor.";
                s.Description = "Retrieves the reading history for a specific sensor over the given number of days.";
                s.Responses[200] = "Reading history retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetReadingsHistoryQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
