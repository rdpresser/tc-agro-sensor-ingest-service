namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorAggregateRepository : IBaseRepository<SensorAggregate>
    {
        Task<SensorAggregate?> GetBySensorIdAsync(string sensorId, CancellationToken ct = default);
        Task<bool> SensorIdExistsAsync(string sensorId, CancellationToken ct = default);
    }
}
