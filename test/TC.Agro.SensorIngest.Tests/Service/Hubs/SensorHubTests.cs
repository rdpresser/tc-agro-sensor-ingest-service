using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SensorIngest.Service.Hubs;

namespace TC.Agro.SensorIngest.Tests.Service.Hubs
{
    public class SensorHubTests
    {
        private readonly ISensorReadingRepository _readingRepository;
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly SensorHub _hub;
        private readonly ISensorHubClient _callerClient;

        public SensorHubTests()
        {
            _readingRepository = A.Fake<ISensorReadingRepository>();
            _snapshotStore = A.Fake<ISensorSnapshotStore>();
            _callerClient = A.Fake<ISensorHubClient>();
            _hub = new SensorHub(_readingRepository, _snapshotStore);

            // Setup Hub context mocks
            var hubCallerContext = A.Fake<HubCallerContext>();
            A.CallTo(() => hubCallerContext.ConnectionId).Returns("test-connection-id");
            _hub.Context = hubCallerContext;

            var clients = A.Fake<IHubCallerClients<ISensorHubClient>>();
            A.CallTo(() => clients.Caller).Returns(_callerClient);
            _hub.Clients = clients;

            var groups = A.Fake<IGroupManager>();
            _hub.Groups = groups;
        }

        #region JoinPlotGroup

        [Fact]
        public async Task JoinPlotGroup_WithValidPlotId_ShouldAddToGroupAndSendRecentReadings()
        {
            var plotId = Guid.NewGuid();
            var sensorId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var propertyId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var label = "Sensor-001";

            var snapshot = SensorSnapshot.Create(
                sensorId,
                ownerId,
                propertyId,
                plotId,
                label,
                "Plot 1",
                "Property 1");

            var readings = new List<SensorReadingAggregate>
            {
                SensorReadingAggregate.Create(sensorId, now, 25.0, 60.0, 40.0, null, 85.0).Value
            };

            A.CallTo(() => _snapshotStore.GetByPlotIdAsync(
                plotId, A<CancellationToken>._))
                .Returns(snapshot);

            A.CallTo(() => _readingRepository.GetByPlotIdAsync(
                plotId, null, null, 20, A<CancellationToken>._))
                .Returns(readings);

            await _hub.JoinPlotGroup(plotId.ToString());

            A.CallTo(() => _hub.Groups.AddToGroupAsync(
                "test-connection-id", $"plot:{plotId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _callerClient.SensorReading(
                A<SensorReadingRequest>.That.Matches(r =>
                    r.SensorId == sensorId &&
                    r.SensorLabel == label)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task JoinPlotGroup_WithNoRecentReadings_ShouldAddToGroupOnly()
        {
            var plotId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var propertyId = Guid.NewGuid();

            var snapshot = SensorSnapshot.Create(
                Guid.NewGuid(),
                ownerId,
                propertyId,
                plotId,
                "Sensor-001",
                "Plot 1",
                "Property 1");

            A.CallTo(() => _snapshotStore.GetByPlotIdAsync(
                plotId, A<CancellationToken>._))
                .Returns(snapshot);

            A.CallTo(() => _readingRepository.GetByPlotIdAsync(
                plotId, null, null, 20, A<CancellationToken>._))
                .Returns(Enumerable.Empty<SensorReadingAggregate>());

            await _hub.JoinPlotGroup(plotId.ToString());

            A.CallTo(() => _hub.Groups.AddToGroupAsync(
                "test-connection-id", $"plot:{plotId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _callerClient.SensorReading(A<SensorReadingRequest>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task JoinPlotGroup_WithInvalidGuid_ShouldThrowHubException()
        {
            await Should.ThrowAsync<HubException>(
                () => _hub.JoinPlotGroup("not-a-guid"));
        }

        [Fact]
        public async Task JoinPlotGroup_WithEmptyGuid_ShouldThrowHubException()
        {
            await Should.ThrowAsync<HubException>(
                () => _hub.JoinPlotGroup(Guid.Empty.ToString()));
        }

        #endregion

        #region LeavePlotGroup

        [Fact]
        public async Task LeavePlotGroup_WithValidPlotId_ShouldRemoveFromGroup()
        {
            var plotId = Guid.NewGuid();

            await _hub.LeavePlotGroup(plotId.ToString());

            A.CallTo(() => _hub.Groups.RemoveFromGroupAsync(
                "test-connection-id", $"plot:{plotId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task LeavePlotGroup_WithInvalidGuid_ShouldThrowHubException()
        {
            await Should.ThrowAsync<HubException>(
                () => _hub.LeavePlotGroup("invalid"));
        }

        [Fact]
        public async Task LeavePlotGroup_WithEmptyGuid_ShouldThrowHubException()
        {
            await Should.ThrowAsync<HubException>(
                () => _hub.LeavePlotGroup(Guid.Empty.ToString()));
        }

        #endregion
    }
}
