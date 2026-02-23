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

        #region SensorOperationalStatusChanged

        private static SensorOperationalStatusChangedIntegrationEvent CreateStatusChangedEvent(
            Guid sensorId,
            string status,
            string? label = "Sensor",
            string plotName = "Plot",
            string propertyName = "Farm")
        {
            var type = typeof(SensorOperationalStatusChangedIntegrationEvent);
            var ctors = type.GetConstructors();
            var ctor = ctors[0];
            for (var i = 1; i < ctors.Length; i++)
            {
                if (ctors[i].GetParameters().Length > ctor.GetParameters().Length)
                    ctor = ctors[i];
            }

            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];

            foreach (var parameter in parameters)
            {
                var name = parameter.Name?.ToLowerInvariant();

                switch (name)
                {
                    case "sensorid":
                        args[parameter.Position] = sensorId;
                        break;
                    case "ownerid":
                    case "changedbyuserid":
                        args[parameter.Position] = Guid.NewGuid();
                        break;
                    case "propertyid":
                    case "plotid":
                        args[parameter.Position] = Guid.NewGuid();
                        break;
                    case "label":
                        args[parameter.Position] = label;
                        break;
                    case "plotname":
                        args[parameter.Position] = plotName;
                        break;
                    case "propertyname":
                        args[parameter.Position] = propertyName;
                        break;
                    case "status":
                        args[parameter.Position] = status;
                        break;
                    case "occurredon":
                        args[parameter.Position] = DateTimeOffset.UtcNow;
                        break;
                    default:
                        if (parameter.ParameterType == typeof(Guid))
                        {
                            args[parameter.Position] = Guid.NewGuid();
                        }
                        else if (parameter.ParameterType == typeof(DateTimeOffset))
                        {
                            args[parameter.Position] = DateTimeOffset.UtcNow;
                        }
                        else
                        {
                            args[parameter.Position] = parameter.HasDefaultValue
                                ? parameter.DefaultValue
                                : null;
                        }
                        break;
                }
            }

            return (SensorOperationalStatusChangedIntegrationEvent)ctor.Invoke(args);
        }

        [Fact]
        public async Task HandleStatusChanged_WithExistingSnapshot_ShouldUpdateSnapshotAndSave()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Old Sensor",
                plotName: "Old Plot",
                propertyName: "Old Farm",
                changedByUserId: Guid.NewGuid(),
                status: "Offline");

            A.CallTo(() => _store.GetByIdAsync(sensorId, A<CancellationToken>._))
                .Returns(snapshot);

            var eventData = CreateStatusChangedEvent(
                sensorId,
                status: "Online",
                label: "New Sensor",
                plotName: "New Plot",
                propertyName: "New Farm");

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            snapshot.Status.ShouldBe("Online");
            snapshot.Label.ShouldBe("New Sensor");
            snapshot.PlotName.ShouldBe("New Plot");
            snapshot.PropertyName.ShouldBe("New Farm");

            A.CallTo(() => _store.UpdateAsync(snapshot, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _store.AddAsync(A<SensorSnapshot>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => _store.DeleteAsync(A<Guid>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleStatusChanged_WithBlankLabel_ShouldUseDefaultLabel()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Old Sensor",
                plotName: "Plot",
                propertyName: "Farm",
                changedByUserId: Guid.NewGuid(),
                status: "Offline");

            A.CallTo(() => _store.GetByIdAsync(sensorId, A<CancellationToken>._))
                .Returns(snapshot);

            var eventData = CreateStatusChangedEvent(
                sensorId,
                status: "Online",
                label: "   ",
                plotName: "Plot",
                propertyName: "Farm");

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            snapshot.Label.ShouldBe("Unnamed Sensor");

            A.CallTo(() => _store.UpdateAsync(snapshot, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleStatusChanged_WhenSnapshotNotFound_ShouldCreateSnapshotAndSave()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _store.GetByIdAsync(sensorId, A<CancellationToken>._))
                .Returns((SensorSnapshot?)null);

            var eventData = CreateStatusChangedEvent(
                sensorId,
                status: "Online",
                label: "Sensor A",
                plotName: "Plot A",
                propertyName: "Farm A");

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.AddAsync(
                A<SensorSnapshot>.That.Matches(s =>
                    s.Id == sensorId &&
                    s.Label == "Sensor A" &&
                    s.PlotName == "Plot A" &&
                    s.PropertyName == "Farm A"),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _store.UpdateAsync(A<SensorSnapshot>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => _store.DeleteAsync(A<Guid>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Theory]
        [InlineData("Online")]
        [InlineData("Warning")]
        [InlineData("Offline")]
        public async Task HandleStatusChanged_ToAnyStatus_ShouldNotDeleteSnapshot(string status)
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor",
                plotName: "Plot",
                propertyName: "Farm",
                changedByUserId: Guid.NewGuid(),
                status: "Online");

            A.CallTo(() => _store.GetByIdAsync(sensorId, A<CancellationToken>._))
                .Returns(snapshot);

            var eventData = CreateStatusChangedEvent(
                sensorId,
                status: status,
                label: "Sensor",
                plotName: "Plot",
                propertyName: "Farm");

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.DeleteAsync(sensorId, A<CancellationToken>._))
                .MustNotHaveHappened();
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
