namespace TC.Agro.SensorIngest.Service.Endpoints.Sensors
{
    public sealed class GetReadingsHistoryEndpoint : BaseApiEndpoint<GetReadingsHistoryQuery, GetReadingsHistoryResponse>
    {
        public override void Configure()
        {
            Get("sensors/{SensorId:guid}/readings");

            // Force FastEndpoints to bind from query parameters
            RequestBinder(new RequestBinder<GetReadingsHistoryQuery>(BindingSource.QueryParams));
            RequestBinder(new RequestBinder<GetReadingsHistoryQuery>(BindingSource.RouteValues));

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
