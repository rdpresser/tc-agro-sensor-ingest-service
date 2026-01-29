namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorReadingReadStore
    {
        Task<IEnumerable<SensorReadingDto>> GetLatestReadingsAsync(
            string? sensorId = null,
            Guid? plotId = null,
            int limit = 10,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<SensorReadingDto>> GetHistoryAsync(
            string sensorId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<HourlyAggregateDto>> GetHourlyAggregatesAsync(
            string sensorId,
            int days = 7,
            CancellationToken cancellationToken = default);
    }

    public sealed record SensorReadingDto(
        Guid Id,
        string SensorId,
        Guid PlotId,
        DateTime Time,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel);

    public sealed record HourlyAggregateDto(
        DateTime Hour,
        double? AvgTemperature,
        double? MaxTemperature,
        double? MinTemperature,
        double? AvgHumidity,
        double? AvgSoilMoisture);
}
