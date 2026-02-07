namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorReadStore
    {
        Task<IReadOnlyList<SensorListDto>> GetSensorsAsync(Guid? plotId, CancellationToken ct);
        Task<SensorDetailDto?> GetByIdAsync(string sensorId, CancellationToken ct);
        Task<int> CountAsync(CancellationToken ct);
    }

    public sealed record SensorListDto(
        Guid Id,
        string SensorId,
        Guid PlotId,
        string PlotName,
        string Status,
        double Battery,
        DateTimeOffset? LastReadingAt,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture);

    public sealed record SensorDetailDto(
        Guid Id,
        string SensorId,
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
