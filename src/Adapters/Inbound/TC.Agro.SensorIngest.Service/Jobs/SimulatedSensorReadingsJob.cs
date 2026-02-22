using Ardalis.Result;
using Bogus;
using Quartz;
using TC.Agro.Contracts.Events.SensorIngested;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SharedKernel.Infrastructure.Messaging;
using Wolverine;

namespace TC.Agro.SensorIngest.Service.Jobs
{
    [DisallowConcurrentExecution]
    internal sealed class SimulatedSensorReadingsJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SimulatedSensorReadingsJob> _logger;

        public SimulatedSensorReadingsJob(IServiceScopeFactory scopeFactory, ILogger<SimulatedSensorReadingsJob> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("SimulatedSensorReadingsJob started");

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var snapshotStore = scope.ServiceProvider.GetRequiredService<ISensorSnapshotStore>();
                var readingRepository = scope.ServiceProvider.GetRequiredService<ISensorReadingRepository>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var hubNotifier = scope.ServiceProvider.GetRequiredService<ISensorHubNotifier>();

                var activeSensors = await snapshotStore.GetAllActiveAsync(context.CancellationToken).ConfigureAwait(false);

                if (activeSensors.Count == 0)
                {
                    _logger.LogInformation("No active sensors found. Skipping reading generation");
                    return;
                }

                _logger.LogInformation("Generating simulated readings for {Count} active sensor(s)", activeSensors.Count);

                var faker = new Faker();
                var now = DateTime.UtcNow;

                var readings = activeSensors
                    .Select(sensor => GenerateReading(faker, sensor.Id, now))
                    .Where(result => result.IsSuccess)
                    .Select(result => result.Value)
                    .ToList();

                if (readings.Count == 0)
                {
                    _logger.LogWarning("No valid readings generated. Skipping persistence");
                    return;
                }

                await readingRepository.AddRangeAsync(readings, context.CancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Persisted {Count} simulated reading(s). Publishing integration events", readings.Count);

                foreach (var reading in readings)
                {
                    var integrationEvent = EventContext<SensorIngestedIntegrationEvent>.CreateBasic<SensorReadingAggregate>(
                        new SensorIngestedIntegrationEvent(
                            reading.Id,
                            reading.SensorId,
                            reading.Time,
                            reading.Temperature,
                            reading.Humidity,
                            reading.SoilMoisture,
                            reading.Rainfall,
                            reading.BatteryLevel,
                            DateTimeOffset.UtcNow),
                        reading.Id);

                    await messageBus.PublishAsync(integrationEvent).ConfigureAwait(false);

                    await hubNotifier.NotifySensorReadingAsync(
                        reading.SensorId,
                        reading.Temperature,
                        reading.Humidity,
                        reading.SoilMoisture,
                        reading.Time).ConfigureAwait(false);
                }

                _logger.LogInformation("SimulatedSensorReadingsJob completed. Generated {Count} reading(s)", readings.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "SimulatedSensorReadingsJob failed");
                throw new JobExecutionException(ex, refireImmediately: false);
            }
        }
        private static Result<SensorReadingAggregate> GenerateReading(Faker faker, Guid sensorId, DateTime now)
        {
            var temperature = Math.Round(faker.Random.Double(15, 40), 2);
            var humidity = Math.Round(faker.Random.Double(30, 90), 2);
            var soilMoisture = Math.Round(faker.Random.Double(10, 80), 2);
            var rainfall = faker.Random.Bool(0.3f) ? Math.Round(faker.Random.Double(0, 50), 2) : (double?)null;
            var batteryLevel = Math.Round(faker.Random.Double(50, 100), 2);

            return SensorReadingAggregate.Create(sensorId, now, temperature, humidity, soilMoisture, rainfall, batteryLevel);
        }
    }
}
