using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Service.Endpoints.Alerts
{
    public sealed class GetAlertListEndpoint : BaseApiEndpoint<GetAlertListQuery, PaginatedResponse<GetAlertListResponse>>
    {
        public override void Configure()
        {
            Get("alerts");
            RequestBinder(new RequestBinder<GetAlertListQuery>(BindingSource.QueryParams));

            PreProcessor<QueryCachingPreProcessorBehavior<GetAlertListQuery, PaginatedResponse<GetAlertListResponse>>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetAlertListQuery, PaginatedResponse<GetAlertListResponse>>>();

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
