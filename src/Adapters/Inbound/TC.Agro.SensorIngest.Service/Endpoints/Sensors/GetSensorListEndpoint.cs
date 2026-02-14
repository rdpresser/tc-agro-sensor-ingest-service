using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Service.Endpoints.Sensors
{
    public sealed class GetSensorListEndpoint : BaseApiEndpoint<GetSensorListQuery, PaginatedResponse<GetSensorListResponse>>
    {
        public override void Configure()
        {
            Get("sensors");
            RequestBinder(new RequestBinder<GetSensorListQuery>(BindingSource.QueryParams));
            PreProcessor<QueryCachingPreProcessorBehavior<GetSensorListQuery, PaginatedResponse<GetSensorListResponse>>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetSensorListQuery, PaginatedResponse<GetSensorListResponse>>>();

            Roles(AppRoles.Admin, AppRoles.Producer);

            Description(
                x => x.Produces<PaginatedResponse<GetSensorListResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets the list of registered sensors.";
                s.Description = "Retrieves all registered sensors with pagination and optional filters.";
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
