namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record GetLatestReadingsResponse(IReadOnlyList<LatestReadingItem> Readings);
}
