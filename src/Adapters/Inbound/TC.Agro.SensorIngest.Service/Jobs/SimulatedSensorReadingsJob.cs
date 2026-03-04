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
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly ISensorReadingRepository _readingRepository;
        private readonly IMessageBus _messageBus;
        private readonly ISensorHubNotifier _hubNotifier;
        private readonly IWeatherDataProvider _weatherProvider;
        private readonly ILogger<SimulatedSensorReadingsJob> _logger;
        private readonly SensorReadingsJobOptions _options;

        public SimulatedSensorReadingsJob(
            ISensorSnapshotStore snapshotStore,
            ISensorReadingRepository readingRepository,
            IMessageBus messageBus,
            ISensorHubNotifier hubNotifier,
            IWeatherDataProvider weatherProvider,
            ILogger<SimulatedSensorReadingsJob> logger,
            IOptions<SensorReadingsJobOptions> options)
        {
            _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
            _readingRepository = readingRepository ?? throw new ArgumentNullException(nameof(readingRepository));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _hubNotifier = hubNotifier ?? throw new ArgumentNullException(nameof(hubNotifier));
            _weatherProvider = weatherProvider ?? throw new ArgumentNullException(nameof(weatherProvider));
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
                var activeSensors = await _snapshotStore.GetAllActiveAsync(context.CancellationToken).ConfigureAwait(false);
                if (activeSensors.Count == 0)
                {
                    _logger.LogInformation("No active sensors found. Skipping reading generation");
                    return;
                }

                var weatherLocations = activeSensors
                    .Select(sensor => BuildWeatherLocation(sensor.PlotLatitude, sensor.PlotLongitude))
                    .Where(location => location is not null)
                    .Select(location => location!)
                    .Distinct()
                    .ToList();

                if (weatherLocations.Count == 0)
                {
                    _logger.LogWarning("No valid plot coordinates found. Falling back to fully simulated weather data");
                }
                else
                {
                    _logger.LogInformation(
                        "Requesting weather from Open-Meteo for {LocationCount} unique location(s)",
                        weatherLocations.Count);
                }

                var weatherByLocation = weatherLocations.Count > 0
                    ? await _weatherProvider.GetCurrentWeatherBatchAsync(weatherLocations, context.CancellationToken).ConfigureAwait(false)
                    : new Dictionary<WeatherLocation, WeatherData>();

                if (weatherByLocation.Count > 0)
                {
                    _logger.LogInformation(
                        "Using real weather data from Open-Meteo for {ResolvedCount}/{RequestedCount} location(s)",
                        weatherByLocation.Count,
                        weatherLocations.Count);
                }
                else if (weatherLocations.Count > 0)
                {
                    _logger.LogWarning("Weather API unavailable or returned no data. Falling back to simulated data per sensor");
                }

                _logger.LogInformation("Generating readings for {Count} active sensor(s)", activeSensors.Count);

                var faker = new Faker();
                var now = DateTime.UtcNow;

                var readings = activeSensors
                    .Select(sensor =>
                    {
                        var weatherLocation = BuildWeatherLocation(sensor.PlotLatitude, sensor.PlotLongitude);
                        var weatherData = weatherLocation is not null
                            && weatherByLocation.TryGetValue(weatherLocation, out var weather)
                                ? weather
                                : null;

                        return GenerateReading(faker, sensor.Id, now, weatherData);
                    })
                    .Where(result => result.IsSuccess)
                    .Select(result => result.Value)
                    .ToList();

                if (readings.Count == 0)
                {
                    _logger.LogWarning("No valid readings generated. Skipping persistence");
                    return;
                }

                // Persist all readings to DB first (AddRangeAsync calls SaveChangesAsync internally)
                await _readingRepository.AddRangeAsync(readings, context.CancellationToken).ConfigureAwait(false);

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

                        await _messageBus.PublishAsync(integrationEvent).ConfigureAwait(false);

                        await _hubNotifier.NotifySensorReadingAsync(
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

        private static WeatherLocation? BuildWeatherLocation(double? latitude, double? longitude)
        {
            if (!latitude.HasValue || !longitude.HasValue)
                return null;

            if (latitude.Value is < -90 or > 90)
                return null;

            if (longitude.Value is < -180 or > 180)
                return null;

            return new WeatherLocation(latitude.Value, longitude.Value);
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
