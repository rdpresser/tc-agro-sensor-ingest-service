using Ardalis.Result;
using Bogus;
using Microsoft.Extensions.Options;
using Quartz;
using TC.Agro.Contracts.Events.SensorIngested;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Infrastructure.Options.Jobs;
using TC.Agro.SharedKernel.Infrastructure.Messaging;

namespace TC.Agro.SensorIngest.Service.Jobs
{
    [DisallowConcurrentExecution]
    internal sealed class SimulatedSensorReadingsJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SimulatedSensorReadingsJob> _logger;
        private readonly SensorReadingsJobOptions _options;

        public SimulatedSensorReadingsJob(
            IServiceScopeFactory scopeFactory,
            ILogger<SimulatedSensorReadingsJob> logger,
            IOptions<SensorReadingsJobOptions> options)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new SensorReadingsJobOptions();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation(
                "SimulatedSensorReadingsJob started (configured interval: {IntervalSeconds}s)",
                _options.IntervalSeconds);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var snapshotStore = scope.ServiceProvider.GetRequiredService<ISensorSnapshotStore>();
                var readingRepository = scope.ServiceProvider.GetRequiredService<ISensorReadingRepository>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var hubNotifier = scope.ServiceProvider.GetRequiredService<ISensorHubNotifier>();
                var weatherProvider = scope.ServiceProvider.GetRequiredService<IWeatherDataProvider>();

                var activeSensors = await snapshotStore.GetAllActiveAsync(context.CancellationToken).ConfigureAwait(false);

                if (activeSensors.Count == 0)
                {
                    _logger.LogInformation("No active sensors found. Skipping reading generation");
                    return;
                }

                var weatherData = await weatherProvider.GetCurrentWeatherAsync(context.CancellationToken).ConfigureAwait(false);

                if (weatherData is not null)
                {
                    _logger.LogInformation(
                        "Using real weather data from Open-Meteo: {Temperature}C, {Humidity}%, SoilMoisture={SoilMoisture}%",
                        weatherData.Temperature, weatherData.Humidity, weatherData.SoilMoisture);
                }
                else
                {
                    _logger.LogWarning("Weather API unavailable, falling back to simulated data");
                }

                _logger.LogInformation("Generating readings for {Count} active sensor(s)", activeSensors.Count);

                var faker = new Faker();
                var now = DateTime.UtcNow;

                var readings = activeSensors
                    .Select(sensor => GenerateReading(faker, sensor.Id, now, weatherData))
                    .Where(result => result.IsSuccess)
                    .Select(result => result.Value)
                    .ToList();

                if (readings.Count == 0)
                {
                    _logger.LogWarning("No valid readings generated. Skipping persistence");
                    return;
                }

                // Persist all readings to DB first (AddRangeAsync calls SaveChangesAsync internally)
                await readingRepository.AddRangeAsync(readings, context.CancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Persisted {Count} reading(s). Publishing integration events", readings.Count);

                var publishFailures = 0;
                foreach (var reading in readings)
                {
                    try
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
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        publishFailures++;
                        _logger.LogWarning(ex, "Failed to publish event/notification for reading {ReadingId}", reading.Id);
                    }
                }

                if (publishFailures > 0)
                    _logger.LogWarning("SimulatedSensorReadingsJob completed with {Failures} publish failure(s) out of {Total}", publishFailures, readings.Count);
                else
                    _logger.LogInformation("SimulatedSensorReadingsJob completed. Generated {Count} reading(s)", readings.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "SimulatedSensorReadingsJob failed");
                throw new JobExecutionException(ex, refireImmediately: false);
            }
        }

        internal static Result<SensorReadingAggregate> GenerateReading(Faker faker, Guid sensorId, DateTime now, WeatherData? weatherData)
        {
            double temperature;
            double humidity;
            double soilMoisture;
            double? rainfall;

            if (weatherData is not null)
            {
                var variance = () => 1.0 + faker.Random.Double(-0.02, 0.02);
                temperature = Math.Round(weatherData.Temperature * variance(), 2);
                humidity = Math.Clamp(Math.Round(weatherData.Humidity * variance(), 2), 0, 100);
                soilMoisture = Math.Clamp(Math.Round(weatherData.SoilMoisture * variance(), 2), 0, 100);
                rainfall = weatherData.Precipitation.HasValue
                    ? Math.Round(weatherData.Precipitation.Value * variance(), 2)
                    : null;
            }
            else
            {
                temperature = Math.Round(faker.Random.Double(15, 40), 2);
                humidity = Math.Round(faker.Random.Double(30, 90), 2);
                soilMoisture = Math.Round(faker.Random.Double(10, 80), 2);
                rainfall = faker.Random.Bool(0.3f) ? Math.Round(faker.Random.Double(0, 50), 2) : null;
            }

            var batteryLevel = Math.Round(faker.Random.Double(50, 100), 2);

            return SensorReadingAggregate.Create(sensorId, now, temperature, humidity, soilMoisture, rainfall, batteryLevel);
        }
    }
}
