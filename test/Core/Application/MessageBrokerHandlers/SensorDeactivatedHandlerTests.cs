using Moq;
using TC.Agro.Contracts.Events.Farm;
using TC.Agro.SensorIngest.Application.MessageBrokerHandlers;
using TC.Agro.SensorIngest.Domain.Aggregates;
using Xunit;

namespace TC.Agro.SensorIngest.Application.Tests.MessageBrokerHandlers
{
    /// <summary>
    /// Unit tests for SensorDeactivatedHandler.
    /// Tests event consumption, cascading deactivation, and idempotency.
    /// </summary>
    public class SensorDeactivatedHandlerTests
    {
        private readonly Mock<ISensorAggregateRepository> _sensorStoreMock;
        private readonly Mock<ISensorSnapshotStore> _snapshotStoreMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<SensorDeactivatedHandler>> _loggerMock;
        private readonly SensorDeactivatedHandler _handler;

        public SensorDeactivatedHandlerTests()
        {
            _sensorStoreMock = new Mock<ISensorAggregateRepository>();
            _snapshotStoreMock = new Mock<ISensorSnapshotStore>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<SensorDeactivatedHandler>>();

            _handler = new SensorDeactivatedHandler(
                _sensorStoreMock.Object,
                _snapshotStoreMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task HandleAsync_ValidEvent_DeactivatesSensor()
        {
            // ARRANGE
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();
            var propertyId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var evt = new SensorDeactivatedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: DateTimeOffset.UtcNow,
                SensorId: sensorId,
                PlotId: plotId,
                PropertyId: propertyId,
                Reason: "Sensor moved to another farm",
                DeactivatedByUserId: userId);

            var eventContext = EventContext<SensorDeactivatedIntegrationEvent>.Create<SensorAggregate>(
                data: evt,
                aggregateId: sensorId,
                userId: userId.ToString(),
                isAuthenticated: true,
                correlationId: "test-corr",
                source: "Test");

            // Create active sensor
            var sensorResult = SensorAggregate.Create(
                sensorId: sensorId,
                plotId: plotId,
                plotName: "Test Plot",
                battery: 85);

            var sensor = sensorResult.Value;
            Assert.True(sensor.IsActive);

            // Create snapshot
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                plotId: plotId,
                propertyId: propertyId,
                plotName: "Test Plot",
                propertyName: "Test Property",
                label: "Test Sensor");
            Assert.True(snapshot.IsActive);

            _sensorStoreMock
                .Setup(x => x.GetBySensorIdAsync(sensorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sensor);

            _snapshotStoreMock
                .Setup(x => x.GetByIdAsync(sensorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(snapshot);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // ACT
            await _handler.HandleAsync(eventContext, CancellationToken.None);

            // ASSERT
            Assert.False(sensor.IsActive);
            Assert.False(snapshot.IsActive);

            _sensorStoreMock.Verify(
                x => x.UpdateAsync(sensor, It.IsAny<CancellationToken>()),
                Times.Once);

            _snapshotStoreMock.Verify(
                x => x.UpdateAsync(snapshot, It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_SensorNotFound_SkipsIdempotently()
        {
            // ARRANGE
            var sensorId = Guid.NewGuid();
            var evt = new SensorDeactivatedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: DateTimeOffset.UtcNow,
                SensorId: sensorId,
                PlotId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                Reason: "Test");

            var eventContext = EventContext<SensorDeactivatedIntegrationEvent>.Create<SensorAggregate>(
                data: evt,
                aggregateId: sensorId,
                userId: null,
                isAuthenticated: false,
                correlationId: "test-corr",
                source: "Test");

            _sensorStoreMock
                .Setup(x => x.GetBySensorIdAsync(sensorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SensorAggregate?)null);

            // ACT
            await _handler.HandleAsync(eventContext, CancellationToken.None);

            // ASSERT (idempotent)
            _sensorStoreMock.Verify(
                x => x.UpdateAsync(It.IsAny<SensorAggregate>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_AlreadyDeactivated_SkipsIdempotently()
        {
            // ARRANGE
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();

            var evt = new SensorDeactivatedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: DateTimeOffset.UtcNow,
                SensorId: sensorId,
                PlotId: plotId,
                PropertyId: Guid.NewGuid(),
                Reason: "Test");

            var eventContext = EventContext<SensorDeactivatedIntegrationEvent>.Create<SensorAggregate>(
                data: evt,
                aggregateId: sensorId,
                userId: null,
                isAuthenticated: false,
                correlationId: "test-corr",
                source: "Test");

            // Create already deactivated sensor
            var sensorResult = SensorAggregate.Create(
                sensorId: sensorId,
                plotId: plotId,
                plotName: "Test Plot",
                battery: 85);

            var sensor = sensorResult.Value;
            sensor.Deactivate();
            Assert.False(sensor.IsActive);

            _sensorStoreMock
                .Setup(x => x.GetBySensorIdAsync(sensorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sensor);

            // ACT
            await _handler.HandleAsync(eventContext, CancellationToken.None);

            // ASSERT (idempotent - no update)
            _sensorStoreMock.Verify(
                x => x.UpdateAsync(It.IsAny<SensorAggregate>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_SnapshotNotFound_StillDeactivatesSensor()
        {
            // ARRANGE
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();

            var evt = new SensorDeactivatedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: DateTimeOffset.UtcNow,
                SensorId: sensorId,
                PlotId: plotId,
                PropertyId: Guid.NewGuid(),
                Reason: "Test");

            var eventContext = EventContext<SensorDeactivatedIntegrationEvent>.Create<SensorAggregate>(
                data: evt,
                aggregateId: sensorId,
                userId: null,
                isAuthenticated: false,
                correlationId: "test-corr",
                source: "Test");

            // Create sensor
            var sensorResult = SensorAggregate.Create(
                sensorId: sensorId,
                plotId: plotId,
                plotName: "Test Plot",
                battery: 85);

            var sensor = sensorResult.Value;

            _sensorStoreMock
                .Setup(x => x.GetBySensorIdAsync(sensorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sensor);

            _snapshotStoreMock
                .Setup(x => x.GetByIdAsync(sensorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SensorSnapshot?)null);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // ACT
            await _handler.HandleAsync(eventContext, CancellationToken.None);

            // ASSERT: Sensor still deactivated even if snapshot not found
            Assert.False(sensor.IsActive);

            _sensorStoreMock.Verify(
                x => x.UpdateAsync(sensor, It.IsAny<CancellationToken>()),
                Times.Once);

            _snapshotStoreMock.Verify(
                x => x.UpdateAsync(It.IsAny<SensorSnapshot>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
