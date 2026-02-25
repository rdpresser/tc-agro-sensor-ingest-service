using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorReadingReadStore
    {
        Task<(IReadOnlyList<GetLatestReadingsResponse> Readings, int TotalCount)> GetLatestReadingsAsync(
            GetLatestReadingsQuery query,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<GetReadingsHistoryResponse> Readings, int TotalCount)> GetHistoryAsync(
            GetReadingsHistoryQuery query,
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
