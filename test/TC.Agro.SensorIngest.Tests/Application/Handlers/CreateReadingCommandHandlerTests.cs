using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using TC.Agro.SensorIngest.Application.Abstractions;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Application.UseCases.CreateReading;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.SensorIngest.Tests.Application.Handlers;

/// <summary>
/// Unit tests for CreateReadingCommandHandler.
/// Covers: sensor validation, aggregate mapping, SignalR notification, integration event publishing.
/// </summary>
public sealed class CreateReadingCommandHandlerTests
{
    private readonly ISensorReadingRepository _repository = A.Fake<ISensorReadingRepository>();
    private readonly IUserContext _userContext = A.Fake<IUserContext>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ISensorHubNotifier _hubNotifier = A.Fake<ISensorHubNotifier>();
    private readonly ISensorSnapshotStore _snapshotStore = A.Fake<ISensorSnapshotStore>();

    public CreateReadingCommandHandlerTests()
    {
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(1));
    }

    // ──────────────────────────────────────────
    // Sensor not registered
    // ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenSensorNotRegistered_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = BuildValidCommand();

        A.CallTo(() => _snapshotStore.ExistsAsync(command.SensorId, ct))
            .Returns(false);

        var handler = CreateHandler();
        var result = await handler.ExecuteAsync(command, ct);

        result.Status.ShouldBe(ResultStatus.NotFound);
        result.Errors.ShouldContain(e => e.Contains(command.SensorId.ToString(), StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.Add(A<SensorReadingAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(ct)).MustNotHaveHappened();
    }

    // ──────────────────────────────────────────
    // Invalid aggregate (bad SensorId)
    // ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenAggregateCreationFails_ShouldReturnInvalid()
    {
        var ct = TestContext.Current.CancellationToken;

        // Empty SensorId makes aggregate creation fail
        var command = new CreateReadingCommand(
            SensorId: Guid.Empty,
            Timestamp: DateTime.UtcNow,
            Temperature: 25,
            Humidity: 60,
            SoilMoisture: 40,
            Rainfall: null,
            BatteryLevel: 85);

        A.CallTo(() => _snapshotStore.ExistsAsync(command.SensorId, ct))
            .Returns(true);

        var handler = CreateHandler();
        var result = await handler.ExecuteAsync(command, ct);

        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    // ──────────────────────────────────────────
    // Successful reading creation
    // ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WithValidCommandAndRegisteredSensor_ShouldCreateReadingAndNotify()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = BuildValidCommand();

        A.CallTo(() => _snapshotStore.ExistsAsync(command.SensorId, ct))
            .Returns(true);

        SensorReadingAggregate? persisted = null;
        A.CallTo(() => _repository.Add(A<SensorReadingAggregate>._))
            .Invokes(call => persisted = call.GetArgument<SensorReadingAggregate>(0));

        var handler = CreateHandler();
        var result = await handler.ExecuteAsync(command, ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.SensorId.ShouldBe(command.SensorId);

        persisted.ShouldNotBeNull();
        persisted!.SensorId.ShouldBe(command.SensorId);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldCallSaveChanges()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = BuildValidCommand();

        A.CallTo(() => _snapshotStore.ExistsAsync(command.SensorId, ct)).Returns(true);

        var handler = CreateHandler();
        await handler.ExecuteAsync(command, ct);

        A.CallTo(() => _outbox.SaveChangesAsync(ct)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldNotifyHubWithCorrectSensorData()
    {
        var ct = TestContext.Current.CancellationToken;
        var sensorId = Guid.NewGuid();
        var command = new CreateReadingCommand(
            SensorId: sensorId,
            Timestamp: DateTime.UtcNow,
            Temperature: 28.5,
            Humidity: 65.0,
            SoilMoisture: 42.0,
            Rainfall: null,
            BatteryLevel: 90.0);

        A.CallTo(() => _snapshotStore.ExistsAsync(sensorId, ct)).Returns(true);

        var handler = CreateHandler();
        await handler.ExecuteAsync(command, ct);

        A.CallTo(() => _hubNotifier.NotifySensorReadingAsync(
            sensorId,
            28.5,
            65.0,
            42.0,
            A<DateTimeOffset>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldEnqueueIntegrationEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = BuildValidCommand();

        A.CallTo(() => _snapshotStore.ExistsAsync(command.SensorId, ct)).Returns(true);

        var handler = CreateHandler();
        await handler.ExecuteAsync(command, ct);

        A.CallTo(_outbox)
            .Where(call => call.Method.Name == "EnqueueAsync")
            .MustHaveHappened();
    }

    // ──────────────────────────────────────────
    // Response shape
    // ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ResponseShouldContainCorrectSensorId()
    {
        var ct = TestContext.Current.CancellationToken;
        var sensorId = Guid.NewGuid();
        var command = new CreateReadingCommand(
            SensorId: sensorId,
            Timestamp: DateTime.UtcNow,
            Temperature: 25,
            Humidity: 60,
            SoilMoisture: 40,
            Rainfall: null,
            BatteryLevel: 85);

        A.CallTo(() => _snapshotStore.ExistsAsync(sensorId, ct)).Returns(true);

        var handler = CreateHandler();
        var result = await handler.ExecuteAsync(command, ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.SensorId.ShouldBe(sensorId);
        result.Value.SensorReadingId.ShouldNotBe(Guid.Empty);
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private CreateReadingCommandHandler CreateHandler()
        => new(_repository, _userContext, _outbox, _hubNotifier, _snapshotStore,
               NullLogger<CreateReadingCommandHandler>.Instance);

    private static CreateReadingCommand BuildValidCommand(Guid? sensorId = null)
        => new(
            SensorId: sensorId ?? Guid.NewGuid(),
            Timestamp: DateTime.UtcNow,
            Temperature: 25.0,
            Humidity: 60.0,
            SoilMoisture: 40.0,
            Rainfall: null,
            BatteryLevel: 85.0);
}
