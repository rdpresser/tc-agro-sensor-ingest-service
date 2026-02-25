using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TC.Agro.Contracts.Events.SensorIngested;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SharedKernel.Application.Ports;

namespace TC.Agro.SensorIngest.Tests.Application.Handlers
{
    public class CreateBatchReadingsCommandHandlerTests
    {
        private readonly ISensorReadingRepository _repository;
        private readonly ITransactionalOutbox _outbox;
        private readonly ISensorSnapshotStore _sensorSnapshotStore;
        private readonly ILogger<CreateBatchReadingsCommandHandler> _logger;
        private readonly CreateBatchReadingsCommandHandler _handler;

        public CreateBatchReadingsCommandHandlerTests()
        {
            _repository = A.Fake<ISensorReadingRepository>();
            _outbox = A.Fake<ITransactionalOutbox>();
            _sensorSnapshotStore = A.Fake<ISensorSnapshotStore>();
            _logger = NullLogger<CreateBatchReadingsCommandHandler>.Instance;
            _handler = new CreateBatchReadingsCommandHandler(_repository, _outbox, _sensorSnapshotStore, _logger);
        }

        private static SensorSnapshot CreateActiveSensor(Guid sensorId)
        {
            return SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Test Sensor",
                plotName: "Test Plot",
                propertyName: "Test Property");
        }

        private static SensorReadingInput CreateValidInput(Guid sensorId, DateTime? timestamp = null)
        {
            return new SensorReadingInput(
                SensorId: sensorId,
                Timestamp: timestamp ?? DateTime.UtcNow.AddMinutes(-1),
                Temperature: 25.0,
                Humidity: 60.0,
                SoilMoisture: 40.0,
                Rainfall: 0.0,
                BatteryLevel: 85.0);
        }

        #region Constructor Validation

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new CreateBatchReadingsCommandHandler(null!, _outbox, _sensorSnapshotStore, _logger));
        }

        [Fact]
        public void Constructor_WithNullOutbox_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new CreateBatchReadingsCommandHandler(_repository, null!, _sensorSnapshotStore, _logger));
        }

        [Fact]
        public void Constructor_WithNullSnapshotStore_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new CreateBatchReadingsCommandHandler(_repository, _outbox, null!, _logger));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new CreateBatchReadingsCommandHandler(_repository, _outbox, _sensorSnapshotStore, null!));
        }

        #endregion

        #region Valid Batch Processing

        [Fact]
        public async Task ExecuteAsync_WithValidBatch_ShouldCreateAggregatesAndReturnSuccess()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var sensor = CreateActiveSensor(sensorId);

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot> { { sensorId, sensor } });

            var command = new CreateBatchReadingsCommand([CreateValidInput(sensorId)]);

            var result = await _handler.ExecuteAsync(command, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ProcessedCount.ShouldBe(1);
            result.Value.FailedCount.ShouldBe(0);
            result.Value.Results.Count.ShouldBe(1);
            result.Value.Results[0].Success.ShouldBeTrue();
            result.Value.Results[0].SensorReadingId.ShouldNotBeNull();
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleValidReadings_ShouldProcessAll()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensor1Id = Guid.NewGuid();
            var sensor2Id = Guid.NewGuid();
            var sensor1 = CreateActiveSensor(sensor1Id);
            var sensor2 = CreateActiveSensor(sensor2Id);

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot>
                {
                    { sensor1Id, sensor1 },
                    { sensor2Id, sensor2 }
                });

            var command = new CreateBatchReadingsCommand([
                CreateValidInput(sensor1Id),
                CreateValidInput(sensor2Id),
                CreateValidInput(sensor1Id, DateTime.UtcNow.AddMinutes(-2))
            ]);

            var result = await _handler.ExecuteAsync(command, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ProcessedCount.ShouldBe(3);
            result.Value.FailedCount.ShouldBe(0);
        }

        #endregion

        #region Unknown Sensor Handling

        [Fact]
        public async Task ExecuteAsync_WithUnknownSensorId_ShouldReportFailure()
        {
            var ct = TestContext.Current.CancellationToken;
            var unknownSensorId = Guid.NewGuid();

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot>());

            var command = new CreateBatchReadingsCommand([CreateValidInput(unknownSensorId)]);

            var result = await _handler.ExecuteAsync(command, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ProcessedCount.ShouldBe(0);
            result.Value.FailedCount.ShouldBe(1);
            result.Value.Results[0].Success.ShouldBeFalse();
            result.Value.Results[0].ErrorMessage!.ShouldContain("not registered");
        }

        [Fact]
        public async Task ExecuteAsync_WithAllUnknownSensors_ShouldNotCallRepository()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot>());

            var command = new CreateBatchReadingsCommand([
                CreateValidInput(Guid.NewGuid()),
                CreateValidInput(Guid.NewGuid())
            ]);

            await _handler.ExecuteAsync(command, ct);

            A.CallTo(() => _repository.AddRangeAsync(A<IEnumerable<SensorReadingAggregate>>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        #endregion

        #region Partial Success

        [Fact]
        public async Task ExecuteAsync_WithMixedValidAndUnknownSensors_ShouldReturnPartialSuccess()
        {
            var ct = TestContext.Current.CancellationToken;
            var validSensorId = Guid.NewGuid();
            var unknownSensorId = Guid.NewGuid();
            var validSensor = CreateActiveSensor(validSensorId);

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot> { { validSensorId, validSensor } });

            var command = new CreateBatchReadingsCommand([
                CreateValidInput(validSensorId),
                CreateValidInput(unknownSensorId)
            ]);

            var result = await _handler.ExecuteAsync(command, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ProcessedCount.ShouldBe(1);
            result.Value.FailedCount.ShouldBe(1);
        }

        #endregion

        #region Repository Interaction

        [Fact]
        public async Task ExecuteAsync_WithValidReadings_ShouldCallRepositoryAddRangeAsync()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var sensor = CreateActiveSensor(sensorId);

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot> { { sensorId, sensor } });

            var command = new CreateBatchReadingsCommand([
                CreateValidInput(sensorId),
                CreateValidInput(sensorId, DateTime.UtcNow.AddMinutes(-2))
            ]);

            await _handler.ExecuteAsync(command, ct);

            A.CallTo(() => _repository.AddRangeAsync(
                A<IEnumerable<SensorReadingAggregate>>.That.Matches(aggs => aggs.Count() == 2),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion

        #region Outbox Integration Events

        [Fact]
        public async Task ExecuteAsync_WithValidReadings_ShouldEnqueueIntegrationEventsForEach()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var sensor = CreateActiveSensor(sensorId);

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot> { { sensorId, sensor } });

            var command = new CreateBatchReadingsCommand([
                CreateValidInput(sensorId),
                CreateValidInput(sensorId, DateTime.UtcNow.AddMinutes(-2))
            ]);

            await _handler.ExecuteAsync(command, ct);

            A.CallTo(() => _outbox.EnqueueAsync(
                A<SensorIngestedIntegrationEvent>._, A<CancellationToken>._))
                .MustHaveHappened(2, Times.Exactly);
        }

        [Fact]
        public async Task ExecuteAsync_WithUnknownSensors_ShouldNotEnqueueEvents()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot>());

            var command = new CreateBatchReadingsCommand([CreateValidInput(Guid.NewGuid())]);

            await _handler.ExecuteAsync(command, ct);

            A.CallTo(() => _outbox.EnqueueAsync(
                A<SensorIngestedIntegrationEvent>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        #endregion

        #region Invalid Aggregate Data

        [Fact]
        public async Task ExecuteAsync_WithInvalidAggregateData_ShouldCollectValidationErrors()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var sensor = CreateActiveSensor(sensorId);

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot> { { sensorId, sensor } });

            // Temperature out of range (>70) and no other required metrics
            var invalidInput = new SensorReadingInput(
                SensorId: sensorId,
                Timestamp: DateTime.UtcNow.AddMinutes(-1),
                Temperature: 100.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var command = new CreateBatchReadingsCommand([invalidInput]);

            var result = await _handler.ExecuteAsync(command, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.FailedCount.ShouldBe(1);
            result.Value.Results[0].Success.ShouldBeFalse();
            result.Value.Results[0].ErrorMessage!.ShouldNotBeNullOrWhiteSpace();
        }

        #endregion

        #region Cancellation Token

        [Fact]
        public async Task ExecuteAsync_ShouldForwardCancellationToken()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var sensor = CreateActiveSensor(sensorId);

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, A<CancellationToken>._))
                .Returns(new Dictionary<Guid, SensorSnapshot> { { sensorId, sensor } });

            var command = new CreateBatchReadingsCommand([CreateValidInput(sensorId)]);

            await _handler.ExecuteAsync(command, ct);

            A.CallTo(() => _sensorSnapshotStore.GetByIdsAsync(A<IEnumerable<Guid>>._, ct))
                .MustHaveHappenedOnceExactly();
        }

        #endregion
    }
}
