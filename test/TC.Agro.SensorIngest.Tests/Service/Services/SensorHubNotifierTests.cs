using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Service.Hubs;
using TC.Agro.SensorIngest.Service.Services;
using TC.Agro.SharedKernel.Infrastructure.Caching.Service;

namespace TC.Agro.SensorIngest.Tests.Service.Services
{
    public class SensorHubNotifierTests
    {
        private readonly IHubContext<SensorHub, ISensorHubClient> _hubContext;
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly ICacheService _cache;
        private readonly ILogger<SensorHubNotifier> _logger;
        private readonly SensorHubNotifier _notifier;
        private readonly ISensorHubClient _client;

        public SensorHubNotifierTests()
        {
            _hubContext = A.Fake<IHubContext<SensorHub, ISensorHubClient>>();
            _snapshotStore = A.Fake<ISensorSnapshotStore>();
            _cache = A.Fake<ICacheService>();
            _logger = NullLogger<SensorHubNotifier>.Instance;
            _client = A.Fake<ISensorHubClient>();
            _notifier = new SensorHubNotifier(_hubContext, _snapshotStore, _cache, _logger);
        }

        #region NotifySensorReadingAsync

        [Fact]
        public async Task NotifySensorReadingAsync_WithExistingSnapshot_ShouldSendToPlotGroup()
        {
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();
            var label = "Sensor-001";

            SetupCacheToReturn(sensorId, plotId);

            A.CallTo(() => _hubContext.Clients.Group($"plot:{plotId}"))
                .Returns(_client);

            var timestamp = DateTimeOffset.UtcNow;

            await _notifier.NotifySensorReadingAsync(sensorId, label, 25.0, 60.0, 40.0, timestamp);

            A.CallTo(() => _client.SensorReading(
                A<SensorReadingRequest>.That.Matches(r =>
                    r.SensorId == sensorId &&
                    r.Label == label &&
                    r.Timestamp == timestamp)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task NotifySensorReadingAsync_WithNoSnapshot_ShouldNotSend()
        {
            var sensorId = Guid.NewGuid();

            SetupCacheToReturn(sensorId, null);

            await _notifier.NotifySensorReadingAsync(sensorId, "Sensor-001", 25.0, 60.0, 40.0, DateTimeOffset.UtcNow);

            A.CallTo(() => _hubContext.Clients.Group(A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task NotifySensorReadingAsync_WhenExceptionThrown_ShouldNotPropagate()
        {
            var sensorId = Guid.NewGuid();

            SetupCacheToThrow(new InvalidOperationException("DB error"));

            var exception = await Record.ExceptionAsync(() =>
                _notifier.NotifySensorReadingAsync(sensorId, "Sensor-001", 25.0, 60.0, 40.0, DateTimeOffset.UtcNow));

            exception.ShouldBeNull();
        }

        #endregion

        #region NotifySensorStatusChangedAsync

        [Fact]
        public async Task NotifySensorStatusChangedAsync_WithExistingSnapshot_ShouldSendToPlotGroup()
        {
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();

            SetupCacheToReturn(sensorId, plotId);

            A.CallTo(() => _hubContext.Clients.Group($"plot:{plotId}"))
                .Returns(_client);

            await _notifier.NotifySensorStatusChangedAsync(sensorId, "Active");

            A.CallTo(() => _client.SensorStatusChanged(
                A<SensorStatusChangedRequest>.That.Matches(r =>
                    r.SensorId == sensorId &&
                    r.Status == "Active")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task NotifySensorStatusChangedAsync_WithNoSnapshot_ShouldNotSend()
        {
            var sensorId = Guid.NewGuid();

            SetupCacheToReturn(sensorId, null);

            await _notifier.NotifySensorStatusChangedAsync(sensorId, "Inactive");

            A.CallTo(() => _hubContext.Clients.Group(A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task NotifySensorStatusChangedAsync_WhenExceptionThrown_ShouldNotPropagate()
        {
            var sensorId = Guid.NewGuid();

            SetupCacheToThrow(new InvalidOperationException("DB error"));

            var exception = await Record.ExceptionAsync(() =>
                _notifier.NotifySensorStatusChangedAsync(sensorId, "Active"));

            exception.ShouldBeNull();
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_WithNullHubContext_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SensorHubNotifier(null!, _snapshotStore, _cache, _logger));
        }

        [Fact]
        public void Constructor_WithNullSnapshotStore_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SensorHubNotifier(_hubContext, null!, _cache, _logger));
        }

        [Fact]
        public void Constructor_WithNullCache_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SensorHubNotifier(_hubContext, _snapshotStore, null!, _logger));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SensorHubNotifier(_hubContext, _snapshotStore, _cache, null!));
        }

        #endregion

        #region Helpers

        private void SetupCacheToReturn(Guid sensorId, Guid? plotId)
        {
            var expectedKey = $"sensor:plotId:{sensorId}";
            A.CallTo(_cache)
                .Where(call => call.Method.Name == "GetOrSetAsync"
                    && call.Arguments.Count > 0
                    && Equals(call.Arguments[0], expectedKey))
                .WithReturnType<ValueTask<Guid?>>()
                .Returns(new ValueTask<Guid?>(plotId));
        }

        private void SetupCacheToThrow(Exception exception)
        {
            A.CallTo(_cache)
                .Where(call => call.Method.Name == "GetOrSetAsync")
                .WithReturnType<ValueTask<Guid?>>()
                .Returns(new ValueTask<Guid?>(Task.FromException<Guid?>(exception)));
        }

        #endregion
    }
}
