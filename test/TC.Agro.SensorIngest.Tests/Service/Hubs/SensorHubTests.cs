using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
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
        private readonly IGroupManager _groups;
        private readonly HubCallerContext _hubCallerContext;

        public SensorHubTests()
        {
            _readingRepository = A.Fake<ISensorReadingRepository>();
            _snapshotStore = A.Fake<ISensorSnapshotStore>();
            _callerClient = A.Fake<ISensorHubClient>();
            _hub = new SensorHub(_readingRepository, _snapshotStore);

            _hubCallerContext = A.Fake<HubCallerContext>();
            A.CallTo(() => _hubCallerContext.ConnectionId).Returns("test-connection-id");
            _hub.Context = _hubCallerContext;

            var clients = A.Fake<IHubCallerClients<ISensorHubClient>>();
            A.CallTo(() => clients.Caller).Returns(_callerClient);
            _hub.Clients = clients;

            _groups = A.Fake<IGroupManager>();
            _hub.Groups = _groups;

            A.CallTo(() => _snapshotStore.GetByOwnerIdAsync(A<Guid>._, A<CancellationToken>._))
                .Returns(Array.Empty<SensorSnapshot>());

            A.CallTo(() => _snapshotStore.GetListByPlotIdAsync(A<Guid>._, A<CancellationToken>._))
                .Returns(Array.Empty<SensorSnapshot>());
        }

        #region JoinPlotGroup

        [Fact]
        public async Task JoinPlotGroup_WithValidPlotId_ShouldAddGroupAndSendRecentReadings()
        {
            var ownerId = Guid.NewGuid();
            var propertyId = Guid.NewGuid();
            var plotId = Guid.NewGuid();
            var sensorId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var snapshot = SensorSnapshot.Create(
                sensorId,
                ownerId,
                propertyId,
                plotId,
                "Sensor-001",
                "Plot 1",
                "Property 1");

            A.CallTo(() => _snapshotStore.GetListByPlotIdAsync(plotId, A<CancellationToken>._))
                .Returns(new[] { snapshot });

            A.CallTo(() => _readingRepository.GetByPlotIdAsync(plotId, null, null, 20, A<CancellationToken>._))
                .Returns(new[] { SensorReadingAggregate.Create(sensorId, now, 24.5, 58.0, 39.0, null, 80.0).Value });

            await _hub.JoinPlotGroup(plotId.ToString());

            A.CallTo(() => _groups.AddToGroupAsync(
                "test-connection-id", $"plot:{plotId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _callerClient.SensorReading(
                A<SensorReadingRequest>.That.Matches(r => r.SensorId == sensorId && r.PlotId == plotId)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task JoinPlotGroup_WithInvalidGuid_ShouldThrowHubException()
        {
            await Should.ThrowAsync<HubException>(() => _hub.JoinPlotGroup("invalid-plot"));
        }

        [Fact]
        public async Task LeavePlotGroup_WithValidPlotId_ShouldRemoveFromGroup()
        {
            var plotId = Guid.NewGuid();

            await _hub.LeavePlotGroup(plotId.ToString());

            A.CallTo(() => _groups.RemoveFromGroupAsync(
                "test-connection-id", $"plot:{plotId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion

        #region JoinOwnerGroup

        [Fact]
        public async Task JoinOwnerGroup_WithAdminRole_ShouldAddGroupAndSendRecentOwnerReadings()
        {
            var ownerId = Guid.NewGuid();
            var sensorId = Guid.NewGuid();
            var propertyId = Guid.NewGuid();
            var plotId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            SetUserContext(new[] { new Claim(ClaimTypes.Role, "Admin") });

            var snapshot = SensorSnapshot.Create(
                sensorId,
                ownerId,
                propertyId,
                plotId,
                "Sensor-001",
                "Plot 1",
                "Property 1");

            var readings = new List<SensorReadingAggregate>
            {
                SensorReadingAggregate.Create(sensorId, now, 25.0, 60.0, 40.0, null, 85.0).Value
            };

            A.CallTo(() => _snapshotStore.GetByOwnerIdAsync(ownerId, A<CancellationToken>._))
                .Returns(new[] { snapshot });

            A.CallTo(() => _readingRepository.GetByPlotIdAsync(
                plotId, null, null, 20, A<CancellationToken>._))
                .Returns(readings);

            await _hub.JoinOwnerGroup(ownerId.ToString());

            A.CallTo(() => _groups.AddToGroupAsync(
                "test-connection-id", $"owner:{ownerId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _groups.AddToGroupAsync(
                "test-connection-id", $"plot:{plotId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _callerClient.SensorReading(
                A<SensorReadingRequest>.That.Matches(r =>
                    r.SensorId == sensorId &&
                    r.SensorLabel == "Sensor-001")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task JoinOwnerGroup_WithProducerRole_ShouldUseOwnerFromClaims()
        {
            var claimOwnerId = Guid.NewGuid();
            var providedOwnerId = Guid.NewGuid();
            SetUserContext(new[]
            {
                new Claim(ClaimTypes.Role, "Producer"),
                new Claim(ClaimTypes.NameIdentifier, claimOwnerId.ToString())
            });

            await _hub.JoinOwnerGroup(providedOwnerId.ToString());

            A.CallTo(() => _groups.AddToGroupAsync(
                "test-connection-id", $"owner:{claimOwnerId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task JoinOwnerGroup_WithAdminRoleAndInvalidOwnerId_ShouldThrowHubException()
        {
            SetUserContext(new[] { new Claim(ClaimTypes.Role, "Admin") });

            await Should.ThrowAsync<HubException>(
                () => _hub.JoinOwnerGroup("not-a-guid"));
        }

        [Fact]
        public async Task JoinOwnerGroup_WithProducerRoleAndMissingOwnerClaim_ShouldThrowHubException()
        {
            SetUserContext(new[] { new Claim(ClaimTypes.Role, "Producer") });

            await Should.ThrowAsync<HubException>(
                () => _hub.JoinOwnerGroup(Guid.NewGuid().ToString()));
        }

        #endregion

        #region LeaveOwnerGroup

        [Fact]
        public async Task LeaveOwnerGroup_WithAdminRole_ShouldRemoveProvidedOwnerGroup()
        {
            var ownerId = Guid.NewGuid();
            var plotId = Guid.NewGuid();
            var snapshot = SensorSnapshot.Create(
                Guid.NewGuid(),
                ownerId,
                Guid.NewGuid(),
                plotId,
                "Sensor-001",
                "Plot 1",
                "Property 1");

            A.CallTo(() => _snapshotStore.GetByOwnerIdAsync(ownerId, A<CancellationToken>._))
                .Returns(new[] { snapshot });

            SetUserContext(new[] { new Claim(ClaimTypes.Role, "Admin") });

            await _hub.LeaveOwnerGroup(ownerId.ToString());

            A.CallTo(() => _groups.RemoveFromGroupAsync(
                "test-connection-id", $"owner:{ownerId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _groups.RemoveFromGroupAsync(
                "test-connection-id", $"plot:{plotId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task LeaveOwnerGroup_WithProducerRole_ShouldRemoveClaimOwnerGroup()
        {
            var claimOwnerId = Guid.NewGuid();
            SetUserContext(new[]
            {
                new Claim(ClaimTypes.Role, "Producer"),
                new Claim("sub", claimOwnerId.ToString())
            });

            await _hub.LeaveOwnerGroup(Guid.NewGuid().ToString());

            A.CallTo(() => _groups.RemoveFromGroupAsync(
                "test-connection-id", $"owner:{claimOwnerId}", A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task LeaveOwnerGroup_WithAdminRoleAndMissingOwnerId_ShouldThrowHubException()
        {
            SetUserContext(new[] { new Claim(ClaimTypes.Role, "Admin") });

            await Should.ThrowAsync<HubException>(
                () => _hub.LeaveOwnerGroup());
        }

        #endregion

        private void SetUserContext(IEnumerable<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, "test-auth", ClaimTypes.NameIdentifier, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            A.CallTo(() => _hubCallerContext.User).Returns(principal);
        }
    }
}
