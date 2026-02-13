namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorAggregateRepository : IBaseRepository<SensorAggregate>
    {
        Task<SensorAggregate?> GetBySensorIdAsync(Guid sensorId, CancellationToken ct = default);
        Task<bool> SensorIdExistsAsync(Guid sensorId, CancellationToken ct = default);
    }
}
