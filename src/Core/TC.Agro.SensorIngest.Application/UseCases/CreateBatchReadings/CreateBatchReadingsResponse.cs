namespace TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings
{
    public sealed record CreateBatchReadingsResponse(
        int ProcessedCount,
        int FailedCount,
        IReadOnlyList<BatchReadingResult> Results);

    public sealed record BatchReadingResult(
        Guid? ReadingId,
        Guid SensorId,
        bool Success,
        string? ErrorMessage = null);
}
