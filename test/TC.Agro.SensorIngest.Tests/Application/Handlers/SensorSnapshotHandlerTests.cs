using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Contracts.Events.Farm;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Application.MessageBrokerHandlers;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.Messaging;

namespace TC.Agro.SensorIngest.Tests.Application.Handlers
{
    public class SensorSnapshotHandlerTests
    {
        private readonly ISensorSnapshotStore _store;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SensorSnapshotHandler> _logger;
        private readonly SensorSnapshotHandler _handler;

        public SensorSnapshotHandlerTests()
        {
            _store = A.Fake<ISensorSnapshotStore>();
            _unitOfWork = A.Fake<IUnitOfWork>();
            _logger = A.Fake<ILogger<SensorSnapshotHandler>>();
            _handler = new SensorSnapshotHandler(_store, _unitOfWork, _logger);
        }

        private static EventContext<T> CreateEvent<T>(T eventData) where T : class
        {
            return EventContext<T>.CreateBasic<SensorReadingAggregate>(eventData, Guid.NewGuid());
        }

        #region SensorRegistered

        [Fact]
        public async Task HandleSensorRegistered_WithValidEvent_ShouldAddSnapshotAndSave()
        {
            var ct = TestContext.Current.CancellationToken;
            var eventData = new SensorRegisteredIntegrationEvent(
                SensorId: Guid.NewGuid(),
                OwnerId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Label: "Sensor North",
                PropertyName: "Farm Alpha",
                PlotName: "Plot 1",
                Type: "Temperature",
                Status: "Active",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.AddAsync(
                A<SensorSnapshot>.That.Matches(s =>
                    s.Id == eventData.SensorId &&
                    s.Label == "Sensor North" &&
                    s.IsActive),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleSensorRegistered_WithBlankLabel_ShouldUseDefaultLabel()
        {
            var ct = TestContext.Current.CancellationToken;
            var eventData = new SensorRegisteredIntegrationEvent(
                SensorId: Guid.NewGuid(),
                OwnerId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Label: "   ",
                PropertyName: "Farm",
                PlotName: "Plot",
                Type: "Humidity",
                Status: "Active",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.AddAsync(
                A<SensorSnapshot>.That.Matches(s => s.Label == "Unnamed Sensor"),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleSensorRegistered_WithNullLabel_ShouldUseDefaultLabel()
        {
            var ct = TestContext.Current.CancellationToken;
            var eventData = new SensorRegisteredIntegrationEvent(
                SensorId: Guid.NewGuid(),
                OwnerId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Label: null,
                PropertyName: "Farm",
                PlotName: "Plot",
                Type: "Temperature",
                Status: "Active",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.AddAsync(
                A<SensorSnapshot>.That.Matches(s => s.Label == "Unnamed Sensor"),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleSensorRegistered_WithNullEvent_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _handler.HandleAsync((EventContext<SensorRegisteredIntegrationEvent>)null!,
                    TestContext.Current.CancellationToken));
        }

        #endregion

        #region SensorDeactivated

        [Fact]
        public async Task HandleSensorDeactivated_ShouldCallDeleteAndSave()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var eventData = new SensorDeactivatedIntegrationEvent(
                SensorId: sensorId,
                PlotId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                Reason: "End of lifecycle",
                DeactivatedByUserId: Guid.NewGuid(),
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.DeleteAsync(sensorId, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleSensorDeactivated_WithNullEvent_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _handler.HandleAsync((EventContext<SensorDeactivatedIntegrationEvent>)null!,
                    TestContext.Current.CancellationToken));
        }

        #endregion

        #region SensorOperationalStatusChanged - Active

        [Fact]
        public async Task HandleStatusChanged_ToActive_WithExistingSnapshot_ShouldReactivateAndUpdate()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                sensorId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Sensor", "Plot", "Farm");
            snapshot.Delete();

            A.CallTo(() => _store.GetByIdIncludingInactiveAsync(sensorId, A<CancellationToken>._))
                .Returns(snapshot);

            var eventData = new SensorOperationalStatusChangedIntegrationEvent(
                SensorId: sensorId,
                PlotId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PreviousStatus: "Maintenance",
                NewStatus: "Active",
                ChangedByUserId: Guid.NewGuid(),
                Reason: "Repair completed",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            snapshot.IsActive.ShouldBeTrue();

            A.CallTo(() => _store.UpdateAsync(snapshot, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _store.DeleteAsync(A<Guid>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleStatusChanged_ToActive_CaseInsensitive_ShouldReactivate()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                sensorId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Sensor", "Plot", "Farm");
            snapshot.Delete();

            A.CallTo(() => _store.GetByIdIncludingInactiveAsync(sensorId, A<CancellationToken>._))
                .Returns(snapshot);

            var eventData = new SensorOperationalStatusChangedIntegrationEvent(
                SensorId: sensorId,
                PlotId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PreviousStatus: "Inactive",
                NewStatus: "ACTIVE",
                ChangedByUserId: Guid.NewGuid(),
                Reason: null,
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            snapshot.IsActive.ShouldBeTrue();
            A.CallTo(() => _store.UpdateAsync(snapshot, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleStatusChanged_ToActive_SnapshotNotFound_ShouldNotThrowAndNotUpdate()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _store.GetByIdIncludingInactiveAsync(sensorId, A<CancellationToken>._))
                .Returns((SensorSnapshot?)null);

            var eventData = new SensorOperationalStatusChangedIntegrationEvent(
                SensorId: sensorId,
                PlotId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PreviousStatus: "Inactive",
                NewStatus: "Active",
                ChangedByUserId: Guid.NewGuid(),
                Reason: null,
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.UpdateAsync(A<SensorSnapshot>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => _store.DeleteAsync(A<Guid>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        #endregion

        #region SensorOperationalStatusChanged - Inactive/Maintenance/Faulty

        [Theory]
        [InlineData("Inactive")]
        [InlineData("Maintenance")]
        [InlineData("Faulty")]
        public async Task HandleStatusChanged_ToNonActiveStatus_ShouldCallDelete(string newStatus)
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var eventData = new SensorOperationalStatusChangedIntegrationEvent(
                SensorId: sensorId,
                PlotId: Guid.NewGuid(),
                PropertyId: Guid.NewGuid(),
                PreviousStatus: "Active",
                NewStatus: newStatus,
                ChangedByUserId: Guid.NewGuid(),
                Reason: "Status change",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.DeleteAsync(sensorId, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _store.GetByIdIncludingInactiveAsync(A<Guid>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleStatusChanged_WithNullEvent_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _handler.HandleAsync((EventContext<SensorOperationalStatusChangedIntegrationEvent>)null!,
                    TestContext.Current.CancellationToken));
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_WithNullStore_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SensorSnapshotHandler(null!, _unitOfWork, _logger));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWork_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SensorSnapshotHandler(_store, null!, _logger));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SensorSnapshotHandler(_store, _unitOfWork, null!));
        }

        #endregion
    }
}
