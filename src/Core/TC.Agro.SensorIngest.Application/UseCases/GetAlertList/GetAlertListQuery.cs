namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    public sealed record GetAlertListQuery(
        string? Status = null) : IBaseQuery<GetAlertListResponse>;
}
