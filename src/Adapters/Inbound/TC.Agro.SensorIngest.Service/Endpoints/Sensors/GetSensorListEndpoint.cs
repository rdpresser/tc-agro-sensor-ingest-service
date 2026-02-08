namespace TC.Agro.SensorIngest.Service.Endpoints.Sensors
{
    public sealed class GetSensorListEndpoint : BaseApiEndpoint<GetSensorListQuery, GetSensorListResponse>
    {
        public override void Configure()
        {
            Get("");
            RoutePrefixOverride("sensors");
            RequestBinder(new RequestBinder<GetSensorListQuery>(BindingSource.QueryParams));
            PreProcessor<QueryCachingPreProcessorBehavior<GetSensorListQuery, GetSensorListResponse>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetSensorListQuery, GetSensorListResponse>>();

            Roles("Admin", "Producer");

            Description(
                x => x.Produces<GetSensorListResponse>(200)
                      .ProducesProblemDetails()
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets the list of registered sensors.";
                s.Description = "Retrieves all registered sensors, optionally filtered by plot ID.";
                s.Responses[200] = "Sensor list retrieved successfully.";
                s.Responses[401] = "Authentication required.";
            });
        }

        public override async Task HandleAsync(GetSensorListQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
