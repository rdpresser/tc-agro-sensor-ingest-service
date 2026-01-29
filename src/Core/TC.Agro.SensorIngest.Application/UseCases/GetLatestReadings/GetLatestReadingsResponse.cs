namespace TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings
{
    public sealed record GetLatestReadingsResponse(IReadOnlyList<SensorReadingDto> Readings);
}
