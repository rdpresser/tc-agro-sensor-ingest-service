namespace TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory
{
    public sealed record GetReadingsHistoryResponse(IReadOnlyList<SensorReadingDto> Readings);
}
