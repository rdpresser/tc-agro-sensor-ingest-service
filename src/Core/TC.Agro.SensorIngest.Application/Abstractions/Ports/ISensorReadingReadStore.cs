using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;
using TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory;

namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorReadingReadStore
    {
        Task<IEnumerable<LatestReadingItem>> GetLatestReadingsAsync(
            string? sensorId = null,
            Guid? plotId = null,
            int limit = 10,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<ReadingHistoryItem>> GetHistoryAsync(
            string sensorId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<HourlyAggregateItem>> GetHourlyAggregatesAsync(
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
