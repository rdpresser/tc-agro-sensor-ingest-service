using Moq;
using TC.Agro.Contracts.Events.Farm;
using TC.Agro.SensorIngest.Application.MessageBrokerHandlers;
using TC.Agro.SensorIngest.Domain.Aggregates;
using Xunit;

namespace TC.Agro.SensorIngest.Application.Tests.MessageBrokerHandlers
{
    /// <summary>
    /// Unit tests for SensorOperationalStatusChangedHandler.
    /// Tests event consumption, state updates, and idempotency.
    /// </summary>
    public class SensorOperationalStatusChangedHandlerTests
    {
        private readonly Mock<ISensorAggregateRepository> _sensorStoreMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<SensorOperationalStatusChangedHandler>> _loggerMock;
        private readonly SensorOperationalStatusChangedHandler _handler;

        public SensorOperationalStatusChangedHandlerTests()
        {
            _sensorStoreMock = new Mock<ISensorAggregateRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<SensorOperationalStatusChangedHandler>>();

            _handler = new SensorOperationalStatusChangedHandler(
                _sensorStoreMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task HandleAsync_ValidEvent_UpdatesSensorStatus()
        {
            // ARRANGE
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var changedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

            var evt = new SensorOperationalStatusChangedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: changedAt,
                SensorId: sensorId,
                PlotId: plotId,
                PropertyId: Guid.NewGuid(),
                PreviousStatus: "Active",
                NewStatus: "Maintenance",
                Reason: "Preventive maintenance",
                ChangedByUserId: userId);

            var eventContext = EventContext<SensorOperationalStatusChangedIntegrationEvent>.Create<SensorAggregate>(
                data: evt,
                aggregateId: sensorId,
                userId: userId.ToString(),
                isAuthenticated: true,
                correlationId: "test-corr-123",
                source: "Test");

            // Create sensor aggregate
            var sensorResult = SensorAggregate.Create(
                sensorId: sensorId,
                plotId: plotId,
                plotName: "Test Plot",
                battery: 85);

            var sensor = sensorResult.Value;

            _sensorStoreMock
                .Setup(x => x.GetBySensorIdAsync(sensorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sensor);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // ACT
            await _handler.HandleAsync(eventContext, CancellationToken.None);

            // ASSERT
            Assert.Equal("Maintenance", sensor.OperationalStatus);
            Assert.Equal("Preventive maintenance", sensor.OperationalStatusReason);
            Assert.Equal(userId, sensor.LastStatusChangedByUserId);
            Assert.NotNull(sensor.LastStatusChangeAt);

            _sensorStoreMock.Verify(
                x => x.UpdateAsync(sensor, It.IsAny<CancellationToken>()),
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
            var evt = new SensorOperationalStatusChangedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: DateTimeOffset.UtcNow,
                SensorId: sensorId,
                PlotId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PreviousStatus: "Active",
                NewStatus: "Maintenance");

            var eventContext = EventContext<SensorOperationalStatusChangedIntegrationEvent>.Create<SensorAggregate>(
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

            // ASSERT (idempotent - no error, no update)
            _sensorStoreMock.Verify(
                x => x.UpdateAsync(It.IsAny<SensorAggregate>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_DuplicateEvent_SkipsIdempotently()
        {
            // ARRANGE
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();
            var changedAt = DateTimeOffset.UtcNow.AddSeconds(-2);

            var evt = new SensorOperationalStatusChangedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: changedAt,
                SensorId: sensorId,
                PlotId: plotId,
                PropertyId: Guid.NewGuid(),
                PreviousStatus: "Active",
                NewStatus: "Maintenance");

            var eventContext = EventContext<SensorOperationalStatusChangedIntegrationEvent>.Create<SensorAggregate>(
                data: evt,
                aggregateId: sensorId,
                userId: null,
                isAuthenticated: false,
                correlationId: "test-corr",
                source: "Test");

            // Create sensor with status already applied
            var sensorResult = SensorAggregate.Create(
                sensorId: sensorId,
                plotId: plotId,
                plotName: "Test Plot",
                battery: 85);

            var sensor = sensorResult.Value;
            sensor.UpdateOperationalStatus("Maintenance", null, changedAt.Subtract(TimeSpan.FromSeconds(1)), null);

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
        public async Task HandleAsync_InvalidEvent_SkipsIdempotently()
        {
            // ARRANGE
            var sensorId = Guid.NewGuid();
            var evt = new SensorOperationalStatusChangedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: DateTimeOffset.UtcNow,
                SensorId: sensorId,
                PlotId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PreviousStatus: "Active",
                NewStatus: "");  // Empty status - invalid

            var eventContext = EventContext<SensorOperationalStatusChangedIntegrationEvent>.Create<SensorAggregate>(
                data: evt,
                aggregateId: sensorId,
                userId: null,
                isAuthenticated: false,
                correlationId: "test-corr",
                source: "Test");

            // ACT
            await _handler.HandleAsync(eventContext, CancellationToken.None);

            // ASSERT (idempotent - no error, no update)
            _sensorStoreMock.Verify(
                x => x.GetBySensorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
