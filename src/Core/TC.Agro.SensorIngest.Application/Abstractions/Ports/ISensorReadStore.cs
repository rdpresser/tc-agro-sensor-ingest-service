using TC.Agro.SensorIngest.Application.UseCases.GetSensorList;

namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorReadStore
    {
        Task<(IReadOnlyList<GetSensorListResponse>, int TotalCount)> GetSensorsAsync(
            GetSensorListQuery query,
            CancellationToken ct);
        Task<SensorDetailItem?> GetByIdAsync(Guid sensorId, CancellationToken ct);
        Task<int> CountAsync(CancellationToken ct);
    }

    public sealed record SensorDetailItem(
        Guid Id,
        Guid SensorId,
        Guid PlotId,
        string PlotName,
        string Status,
        double Battery,
        DateTimeOffset? LastReadingAt,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        DateTimeOffset CreatedAt);
}
