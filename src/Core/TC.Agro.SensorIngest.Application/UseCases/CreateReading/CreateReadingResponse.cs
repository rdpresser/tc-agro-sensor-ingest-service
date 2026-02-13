namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    public sealed record CreateReadingResponse(
        Guid ReadingId,
        Guid SensorId,
        Guid PlotId,
        DateTime Timestamp,
        string Message = "Reading received successfully");
}
