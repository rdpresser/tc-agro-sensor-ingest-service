namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    public sealed record CreateReadingResponse(
        Guid ReadingId,
        string SensorId,
        Guid PlotId,
        DateTime Timestamp,
        string Message = "Reading received successfully");
}
