namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface ISensorReadingRepository : IBaseRepository<SensorReadingAggregate>
    {
        Task<IEnumerable<SensorReadingAggregate>> GetLatestBySensorIdAsync(
            string sensorId,
            int limit = 10,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<SensorReadingAggregate>> GetByPlotIdAsync(
            Guid plotId,
            DateTime? from = null,
            DateTime? to = null,
            int limit = 100,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IEnumerable<SensorReadingAggregate> readings,
            CancellationToken cancellationToken = default);
    }
}
