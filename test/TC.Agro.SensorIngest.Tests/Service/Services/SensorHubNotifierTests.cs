using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Domain.Snapshots;
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

        public SensorHubNotifierTests()
        {
            _hubContext = A.Fake<IHubContext<SensorHub, ISensorHubClient>>();
            _snapshotStore = A.Fake<ISensorSnapshotStore>();
            _cache = A.Fake<ICacheService>();
            _logger = NullLogger<SensorHubNotifier>.Instance;
            _notifier = new SensorHubNotifier(_hubContext, _snapshotStore, _cache, _logger);
        }

        #region NotifySensorReadingAsync

        [Fact]
        public async Task NotifySensorReadingAsync_WithExistingSnapshot_ShouldNotThrow()
        {
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();
            var label = "Sensor-001";

            var snapshot = SensorSnapshot.Create(
                sensorId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                plotId,
                label,
                "Plot 1",
                "Property 1");

            A.CallTo(_cache)
                .Where(call => call.Method.Name == "GetOrSetAsync"
                    && call.Arguments.Count >= 1
                    && Equals(call.Arguments[0], $"sensor:plotId:{sensorId}"))
                .WithReturnType<ValueTask<SensorSnapshot?>>()
                .Returns(new ValueTask<SensorSnapshot?>(snapshot));

            var timestamp = DateTimeOffset.UtcNow;

            // Should not throw
            var exception = await Record.ExceptionAsync(() =>
                _notifier.NotifySensorReadingAsync(sensorId, 25.0, 60.0, 40.0, timestamp));

            exception.ShouldBeNull();
        }

        [Fact]
        public async Task NotifySensorReadingAsync_WithNoSnapshot_ShouldNotSend()
        {
            var sensorId = Guid.NewGuid();

            A.CallTo(_cache)
                .Where(call => call.Method.Name == "GetOrSetAsync"
                    && call.Arguments.Count >= 1
                    && Equals(call.Arguments[0], $"sensor:plotId:{sensorId}"))
                .WithReturnType<ValueTask<SensorSnapshot?>>()
                .Returns(new ValueTask<SensorSnapshot?>((SensorSnapshot?)null));

            // Should not throw
            var exception = await Record.ExceptionAsync(() =>
                _notifier.NotifySensorReadingAsync(sensorId, 25.0, 60.0, 40.0, DateTimeOffset.UtcNow));

            exception.ShouldBeNull();
        }

        [Fact]
        public async Task NotifySensorReadingAsync_WhenExceptionThrown_ShouldNotPropagate()
        {
            var sensorId = Guid.NewGuid();

            A.CallTo(_cache)
                .Where(call => call.Method.Name == "GetOrSetAsync")
                .WithReturnType<ValueTask<SensorSnapshot?>>()
                .Throws(new InvalidOperationException("DB error"));

            var exception = await Record.ExceptionAsync(() =>
                _notifier.NotifySensorReadingAsync(sensorId, 25.0, 60.0, 40.0, DateTimeOffset.UtcNow));

            exception.ShouldBeNull();
        }

        #endregion

        #region NotifySensorStatusChangedAsync

        [Fact]
        public async Task NotifySensorStatusChangedAsync_WithExistingSnapshot_ShouldNotThrow()
        {
            var sensorId = Guid.NewGuid();
            var plotId = Guid.NewGuid();

            var snapshot = SensorSnapshot.Create(
                sensorId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                plotId,
                "Sensor-001",
                "Plot 1",
                "Property 1");

            A.CallTo(_cache)
                .Where(call => call.Method.Name == "GetOrSetAsync"
                    && call.Arguments.Count >= 1
                    && Equals(call.Arguments[0], $"sensor:plotId:{sensorId}"))
                .WithReturnType<ValueTask<SensorSnapshot?>>()
                .Returns(new ValueTask<SensorSnapshot?>(snapshot));

            // Should not throw
            var exception = await Record.ExceptionAsync(() =>
                _notifier.NotifySensorStatusChangedAsync(sensorId, "Active"));

            exception.ShouldBeNull();
        }

        [Fact]
        public async Task NotifySensorStatusChangedAsync_WithNoSnapshot_ShouldNotSend()
        {
            var sensorId = Guid.NewGuid();

            A.CallTo(_cache)
                .Where(call => call.Method.Name == "GetOrSetAsync"
                    && call.Arguments.Count >= 1
                    && Equals(call.Arguments[0], $"sensor:plotId:{sensorId}"))
                .WithReturnType<ValueTask<SensorSnapshot?>>()
                .Returns(new ValueTask<SensorSnapshot?>((SensorSnapshot?)null));

            // Should not throw
            var exception = await Record.ExceptionAsync(() =>
                _notifier.NotifySensorStatusChangedAsync(sensorId, "Inactive"));

            exception.ShouldBeNull();
        }

        [Fact]
        public async Task NotifySensorStatusChangedAsync_WhenExceptionThrown_ShouldNotPropagate()
        {
            var sensorId = Guid.NewGuid();

            A.CallTo(_cache)
                .Where(call => call.Method.Name == "GetOrSetAsync")
                .WithReturnType<ValueTask<SensorSnapshot?>>()
                .Throws(new InvalidOperationException("DB error"));

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
    }
}
