using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Service.Endpoints.Dashboard
{
    public sealed class GetDashboardLatestEndpoint : BaseApiEndpoint<GetLatestReadingsQuery, PaginatedResponse<GetLatestReadingsResponse>>
    {
        public override void Configure()
        {
            Get("dashboard/latest");
            RequestBinder(new RequestBinder<GetLatestReadingsQuery>(BindingSource.QueryParams));
            PreProcessor<QueryCachingPreProcessorBehavior<GetLatestReadingsQuery, PaginatedResponse<GetLatestReadingsResponse>>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetLatestReadingsQuery, PaginatedResponse<GetLatestReadingsResponse>>>();

            Roles(AppRoles.Admin, AppRoles.Producer);

            Description(
                x => x.Produces<PaginatedResponse<GetLatestReadingsResponse>>(200)
                    .ProducesProblemDetails()
                    .Produces(401));

            Summary(s =>
            {
                s.Summary = "Gets latest readings for dashboard.";
                s.Description = "Retrieves the most recent sensor readings for the dashboard overview.";
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
