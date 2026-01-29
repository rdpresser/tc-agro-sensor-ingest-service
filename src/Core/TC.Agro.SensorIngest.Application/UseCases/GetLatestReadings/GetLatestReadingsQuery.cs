namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record GetLatestReadingsQuery(
        string? SensorId = null,
        Guid? PlotId = null,
        int Limit = 10) : IBaseQuery<GetLatestReadingsResponse>;
}
