using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quartz;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SensorIngest.Infrastructure.Options.Jobs;
using TC.Agro.SensorIngest.Service.Jobs;
using Wolverine;

namespace TC.Agro.SensorIngest.Tests.Service.Jobs
{
    public class SimulatedSensorReadingsJobTests
    {
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly ISensorReadingRepository _readingRepository;
        private readonly IMessageBus _messageBus;
        private readonly ISensorHubNotifier _hubNotifier;
        private readonly IWeatherDataProvider _weatherProvider;
        private readonly ILogger<SimulatedSensorReadingsJob> _logger;
        private readonly IOptions<SensorReadingsJobOptions> _jobOptions;
        private readonly SimulatedSensorReadingsJob _job;
        private readonly IJobExecutionContext _jobContext;

        public SimulatedSensorReadingsJobTests()
        {
            _snapshotStore = A.Fake<ISensorSnapshotStore>();
            _readingRepository = A.Fake<ISensorReadingRepository>();
            _messageBus = A.Fake<IMessageBus>();
            _hubNotifier = A.Fake<ISensorHubNotifier>();
            _weatherProvider = A.Fake<IWeatherDataProvider>();
            _logger = NullLogger<SimulatedSensorReadingsJob>.Instance;
            _jobOptions = Options.Create(new SensorReadingsJobOptions { Enabled = true, IntervalSeconds = 5 });
            _jobContext = A.Fake<IJobExecutionContext>();

            A.CallTo(() => _jobContext.CancellationToken).Returns(CancellationToken.None);
            A.CallTo(() => _weatherProvider.GetCurrentWeatherBatchAsync(
                    A<IReadOnlyCollection<WeatherLocation>>._,
                    A<CancellationToken>._))
                .Returns(new Dictionary<WeatherLocation, WeatherData>());

            _job = new SimulatedSensorReadingsJob(
                _snapshotStore,
                _readingRepository,
                _messageBus,
                _hubNotifier,
                _weatherProvider,
                _logger,
                _jobOptions);
        }

        #region Execute - No Active Sensors

        [Fact]
        public async Task Execute_WithNoActiveSensors_ShouldSkipGeneration()
        {
            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot>());

            await _job.Execute(_jobContext);

            A.CallTo(() => _readingRepository.AddRangeAsync(
                A<IEnumerable<SensorReadingAggregate>>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(_messageBus)
                .Where(call => call.Method.Name == "PublishAsync")
                .MustNotHaveHappened();
        }

        #endregion

        #region Execute - With Active Sensors

        [Fact]
        public async Task Execute_WithActiveSensors_ShouldGenerateAndPersistReadings()
        {
            var sensorId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor",
                plotName: "Plot",
                propertyName: "Farm");

            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot> { snapshot });

            await _job.Execute(_jobContext);

            A.CallTo(() => _readingRepository.AddRangeAsync(
                A<IEnumerable<SensorReadingAggregate>>.That.Matches(r => r.Any()),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Execute_WithActiveSensors_ShouldPublishIntegrationEvents()
        {
            var sensorId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor",
                plotName: "Plot",
                propertyName: "Farm");

            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot> { snapshot });

            await _job.Execute(_jobContext);

            A.CallTo(_messageBus)
                .Where(call => call.Method.Name == "PublishAsync")
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Execute_WithActiveSensors_ShouldNotifyViaSignalR()
        {
            var sensorId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor",
                plotName: "Plot",
                propertyName: "Farm");

            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot> { snapshot });

            await _job.Execute(_jobContext);

            A.CallTo(() => _hubNotifier.NotifySensorReadingAsync(
                sensorId,
                A<double?>._, A<double?>._, A<double?>._,
                A<DateTimeOffset>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Execute_WithMultipleSensors_ShouldGenerateReadingForEach()
        {
            var sensor1 = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor 1",
                plotName: "Plot",
                propertyName: "Farm");
            var sensor2 = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor 2",
                plotName: "Plot",
                propertyName: "Farm");
            var sensor3 = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor 3",
                plotName: "Plot",
                propertyName: "Farm");

            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot> { sensor1, sensor2, sensor3 });

            await _job.Execute(_jobContext);

            A.CallTo(() => _readingRepository.AddRangeAsync(
                A<IEnumerable<SensorReadingAggregate>>.That.Matches(r => r.Count() == 3),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(_messageBus)
                .Where(call => call.Method.Name == "PublishAsync")
                .MustHaveHappened(3, Times.Exactly);

            A.CallTo(() => _hubNotifier.NotifySensorReadingAsync(
                A<Guid>._, A<double?>._, A<double?>._, A<double?>._, A<DateTimeOffset>._))
                .MustHaveHappened(3, Times.Exactly);
        }

        [Fact]
        public async Task Execute_WithRealWeatherData_ShouldUseWeatherValues()
        {
            var sensorId = Guid.NewGuid();
            var location = new WeatherLocation(-22.7256, -47.6492);
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor",
                plotName: "Plot",
                propertyName: "Farm",
                plotLatitude: location.Latitude,
                plotLongitude: location.Longitude);

            var weatherData = new WeatherData(25.0, 60.0, 30.0, 2.5);

            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot> { snapshot });
            A.CallTo(() => _weatherProvider.GetCurrentWeatherBatchAsync(
                    A<IReadOnlyCollection<WeatherLocation>>._,
                    A<CancellationToken>._))
                .Returns(new Dictionary<WeatherLocation, WeatherData>
                {
                    [location] = weatherData
                });

            await _job.Execute(_jobContext);

            A.CallTo(() => _readingRepository.AddRangeAsync(
                A<IEnumerable<SensorReadingAggregate>>.That.Matches(r =>
                    r.Any(reading =>
                        reading.Temperature >= 24.0 && reading.Temperature <= 26.0 &&
                        reading.Humidity >= 58.0 && reading.Humidity <= 62.0 &&
                        reading.SoilMoisture >= 29.0 && reading.SoilMoisture <= 31.0)),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Execute_WhenBatchWeatherReturnsEmpty_ShouldFallbackToSimulated()
        {
            var sensorId = Guid.NewGuid();
            var location = new WeatherLocation(-22.7256, -47.6492);
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor",
                plotName: "Plot",
                propertyName: "Farm",
                plotLatitude: location.Latitude,
                plotLongitude: location.Longitude);

            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot> { snapshot });
            A.CallTo(() => _weatherProvider.GetCurrentWeatherBatchAsync(
                    A<IReadOnlyCollection<WeatherLocation>>._,
                    A<CancellationToken>._))
                .Returns(new Dictionary<WeatherLocation, WeatherData>());

            await _job.Execute(_jobContext);

            A.CallTo(() => _readingRepository.AddRangeAsync(
                A<IEnumerable<SensorReadingAggregate>>.That.Matches(r => r.Any()),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Execute_WithSensorsFromSameLocation_ShouldRequestWeatherOnceForUniqueLocation()
        {
            var location = new WeatherLocation(-22.7256, -47.6492);
            var sensor1 = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor 1",
                plotName: "Plot",
                propertyName: "Farm",
                plotLatitude: location.Latitude,
                plotLongitude: location.Longitude);

            var sensor2 = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor 2",
                plotName: "Plot",
                propertyName: "Farm",
                plotLatitude: location.Latitude,
                plotLongitude: location.Longitude);

            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot> { sensor1, sensor2 });

            A.CallTo(() => _weatherProvider.GetCurrentWeatherBatchAsync(
                    A<IReadOnlyCollection<WeatherLocation>>._,
                    A<CancellationToken>._))
                .Returns(new Dictionary<WeatherLocation, WeatherData>
                {
                    [location] = new WeatherData(25.0, 60.0, 30.0, 1.0)
                });

            await _job.Execute(_jobContext);

            A.CallTo(() => _weatherProvider.GetCurrentWeatherBatchAsync(
                    A<IReadOnlyCollection<WeatherLocation>>.That.Matches(locations => locations.Count == 1),
                    A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion

        #region Execute - Error Handling

        [Fact]
        public async Task Execute_WhenSnapshotStoreThrows_ShouldWrapInJobExecutionException()
        {
            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Throws(new InvalidOperationException("DB connection failed"));

            var exception = await Should.ThrowAsync<JobExecutionException>(
                () => _job.Execute(_jobContext));

            exception.InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task Execute_WhenCancelled_ShouldPropagateOperationCancelledException()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            A.CallTo(() => _jobContext.CancellationToken).Returns(cts.Token);
            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .ThrowsAsync(new OperationCanceledException());

            await Should.ThrowAsync<OperationCanceledException>(
                () => _job.Execute(_jobContext));
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_WithNullSnapshotStore_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SimulatedSensorReadingsJob(
                    null!,
                    _readingRepository,
                    _messageBus,
                    _hubNotifier,
                    _weatherProvider,
                    _logger,
                    _jobOptions));
        }

        [Fact]
        public void Constructor_WithNullReadingRepository_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SimulatedSensorReadingsJob(
                    _snapshotStore,
                    null!,
                    _messageBus,
                    _hubNotifier,
                    _weatherProvider,
                    _logger,
                    _jobOptions));
        }

        [Fact]
        public void Constructor_WithNullMessageBus_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SimulatedSensorReadingsJob(
                    _snapshotStore,
                    _readingRepository,
                    null!,
                    _hubNotifier,
                    _weatherProvider,
                    _logger,
                    _jobOptions));
        }

        [Fact]
        public void Constructor_WithNullHubNotifier_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SimulatedSensorReadingsJob(
                    _snapshotStore,
                    _readingRepository,
                    _messageBus,
                    null!,
                    _weatherProvider,
                    _logger,
                    _jobOptions));
        }

        [Fact]
        public void Constructor_WithNullWeatherProvider_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SimulatedSensorReadingsJob(
                    _snapshotStore,
                    _readingRepository,
                    _messageBus,
                    _hubNotifier,
                    null!,
                    _logger,
                    _jobOptions));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SimulatedSensorReadingsJob(
                    _snapshotStore,
                    _readingRepository,
                    _messageBus,
                    _hubNotifier,
                    _weatherProvider,
                    null!,
                    _jobOptions));
        }

        #endregion

        #region GenerateReading - Unit Tests

        [Fact]
        public void GenerateReading_WithWeatherData_ShouldApplySmallVariance()
        {
            var faker = new Bogus.Faker();
            var sensorId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var weatherData = new WeatherData(28.5, 65.0, 35.0, 5.0);

            var result = SimulatedSensorReadingsJob.GenerateReading(faker, sensorId, now, weatherData);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Temperature!.Value.ShouldBeInRange(27.93, 29.07);
            result.Value.Humidity!.Value.ShouldBeInRange(63.7, 66.3);
            result.Value.SoilMoisture!.Value.ShouldBeInRange(34.3, 35.7);
            result.Value.Rainfall.ShouldNotBeNull();
            result.Value.Rainfall!.Value.ShouldBeInRange(4.9, 5.1);
        }

        [Fact]
        public void GenerateReading_WithNullWeatherData_ShouldUseBogusRanges()
        {
            var faker = new Bogus.Faker();
            var sensorId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var result = SimulatedSensorReadingsJob.GenerateReading(faker, sensorId, now, null);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Temperature!.Value.ShouldBeInRange(15, 40);
            result.Value.Humidity!.Value.ShouldBeInRange(30, 90);
            result.Value.SoilMoisture!.Value.ShouldBeInRange(10, 80);
            result.Value.BatteryLevel!.Value.ShouldBeInRange(50, 100);
        }

        [Fact]
        public void GenerateReading_WithWeatherDataNoPrecipitation_ShouldHaveNullRainfall()
        {
            var faker = new Bogus.Faker();
            var sensorId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var weatherData = new WeatherData(25.0, 60.0, 30.0, null);

            var result = SimulatedSensorReadingsJob.GenerateReading(faker, sensorId, now, weatherData);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Rainfall.ShouldBeNull();
        }

        #endregion
    }
}
