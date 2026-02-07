namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    public sealed record GetReadingsHistoryQuery(
        string SensorId,
        int Days = 7) : IBaseQuery<GetReadingsHistoryResponse>;
}
