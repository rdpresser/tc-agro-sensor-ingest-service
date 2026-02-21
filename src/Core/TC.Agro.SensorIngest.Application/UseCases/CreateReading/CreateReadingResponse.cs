namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    public sealed record CreateReadingResponse(
        Guid SensorReadingId,
        Guid SensorId,
        DateTime Timestamp,
        string Message = "Reading received successfully");
}
