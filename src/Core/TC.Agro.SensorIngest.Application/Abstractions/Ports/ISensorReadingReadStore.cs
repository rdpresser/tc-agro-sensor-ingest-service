namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorReadingReadStore
    {
        Task<IReadOnlyList<LatestReadingItem>> GetLatestReadingsAsync(
            Guid? sensorId = null,
            Guid? plotId = null,
            int limit = 10,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ReadingHistoryItem>> GetHistoryAsync(
            Guid sensorId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<HourlyAggregateItem>> GetHourlyAggregatesAsync(
            string sensorId,
            int days = 7,
            CancellationToken cancellationToken = default);
    }

    public sealed record HourlyAggregateItem(
        DateTime Hour,
        double? AvgTemperature,
        double? MaxTemperature,
        double? MinTemperature,
        double? AvgHumidity,
        double? AvgSoilMoisture);
}
