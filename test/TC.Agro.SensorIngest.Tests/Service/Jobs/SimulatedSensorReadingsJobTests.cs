using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SensorIngest.Service.Jobs;
using Wolverine;

namespace TC.Agro.SensorIngest.Tests.Service.Jobs
{
    public class SimulatedSensorReadingsJobTests
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceScope _scope;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly ISensorReadingRepository _readingRepository;
        private readonly IMessageBus _messageBus;
        private readonly ISensorHubNotifier _hubNotifier;
        private readonly IWeatherDataProvider _weatherProvider;
        private readonly ILogger<SimulatedSensorReadingsJob> _logger;
        private readonly SimulatedSensorReadingsJob _job;
        private readonly IJobExecutionContext _jobContext;

        public SimulatedSensorReadingsJobTests()
        {
            _scopeFactory = A.Fake<IServiceScopeFactory>();
            _scope = A.Fake<IServiceScope>();
            _serviceProvider = A.Fake<IServiceProvider>();
            _snapshotStore = A.Fake<ISensorSnapshotStore>();
            _readingRepository = A.Fake<ISensorReadingRepository>();
            _messageBus = A.Fake<IMessageBus>();
            _hubNotifier = A.Fake<ISensorHubNotifier>();
            _weatherProvider = A.Fake<IWeatherDataProvider>();
            _logger = NullLogger<SimulatedSensorReadingsJob>.Instance;
            _jobContext = A.Fake<IJobExecutionContext>();

            A.CallTo(() => _scopeFactory.CreateScope()).Returns(_scope);
            A.CallTo(() => _scope.ServiceProvider).Returns(_serviceProvider);
            A.CallTo(() => _serviceProvider.GetService(typeof(ISensorSnapshotStore))).Returns(_snapshotStore);
            A.CallTo(() => _serviceProvider.GetService(typeof(ISensorReadingRepository))).Returns(_readingRepository);
            A.CallTo(() => _serviceProvider.GetService(typeof(IMessageBus))).Returns(_messageBus);
            A.CallTo(() => _serviceProvider.GetService(typeof(ISensorHubNotifier))).Returns(_hubNotifier);
            A.CallTo(() => _serviceProvider.GetService(typeof(IWeatherDataProvider))).Returns(_weatherProvider);
            A.CallTo(() => _jobContext.CancellationToken).Returns(CancellationToken.None);

            _job = new SimulatedSensorReadingsJob(_scopeFactory, _logger);
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
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor",
                plotName: "Plot",
                propertyName: "Farm");

            var weatherData = new WeatherData(25.0, 60.0, 30.0, 2.5);

            A.CallTo(() => _snapshotStore.GetAllActiveAsync(A<CancellationToken>._))
                .Returns(new List<SensorSnapshot> { snapshot });
            A.CallTo(() => _weatherProvider.GetCurrentWeatherAsync(A<CancellationToken>._))
                .Returns(weatherData);

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
        public async Task Execute_WhenWeatherProviderReturnsNull_ShouldFallbackToSimulated()
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
            A.CallTo(() => _weatherProvider.GetCurrentWeatherAsync(A<CancellationToken>._))
                .Returns((WeatherData?)null);

            await _job.Execute(_jobContext);

            A.CallTo(() => _readingRepository.AddRangeAsync(
                A<IEnumerable<SensorReadingAggregate>>.That.Matches(r => r.Any()),
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
        public void Constructor_WithNullScopeFactory_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SimulatedSensorReadingsJob(null!, _logger));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SimulatedSensorReadingsJob(_scopeFactory, null!));
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
