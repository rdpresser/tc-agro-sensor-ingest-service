using FakeItEasy;
using TC.Agro.Contracts.Events.Identity;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Application.MessageBrokerHandlers;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.Messaging;

namespace TC.Agro.SensorIngest.Tests.Application.Handlers
{
    public class OwnerSnapshotHandlerTests
    {
        private readonly IOwnerSnapshotStore _store;
        private readonly IUnitOfWork _unitOfWork;
        private readonly OwnerSnapshotHandler _handler;

        public OwnerSnapshotHandlerTests()
        {
            _store = A.Fake<IOwnerSnapshotStore>();
            _unitOfWork = A.Fake<IUnitOfWork>();
            _handler = new OwnerSnapshotHandler(_store, _unitOfWork);
        }

        private static EventContext<T> CreateEvent<T>(T eventData) where T : class
        {
            return EventContext<T>.CreateBasic<SensorReadingAggregate>(eventData, Guid.NewGuid());
        }

        #region Constructor Validation

        [Fact]
        public void Constructor_WithNullStore_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new OwnerSnapshotHandler(null!, _unitOfWork));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWork_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new OwnerSnapshotHandler(_store, null!));
        }

        #endregion

        #region UserCreated - Null Check

        [Fact]
        public async Task HandleUserCreated_WithNullEvent_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _handler.HandleAsync((EventContext<UserCreatedIntegrationEvent>)null!,
                    TestContext.Current.CancellationToken));
        }

        #endregion

        #region UserCreated - Role Filtering

        [Theory]
        [InlineData("Admin")]
        [InlineData("User")]
        [InlineData("Sensor")]
        [InlineData("unknown")]
        public async Task HandleUserCreated_WithNonProducerRole_ShouldSkip(string role)
        {
            var ct = TestContext.Current.CancellationToken;
            var eventData = new UserCreatedIntegrationEvent(
                OwnerId: Guid.NewGuid(),
                Name: "Test User",
                Email: "test@example.com",
                Username: "testuser",
                Role: role,
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.AddAsync(A<OwnerSnapshot>._, A<CancellationToken>._))
                .MustNotHaveHappened();
            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        #endregion

        #region UserCreated - Idempotency

        [Fact]
        public async Task HandleUserCreated_WithExistingSnapshot_ShouldSkipDuplicate()
        {
            var ct = TestContext.Current.CancellationToken;
            var ownerId = Guid.NewGuid();
            var existing = OwnerSnapshot.Create(ownerId, "Existing", "existing@example.com");

            A.CallTo(() => _store.GetByIdAsync(ownerId, A<CancellationToken>._))
                .Returns(existing);

            var eventData = new UserCreatedIntegrationEvent(
                OwnerId: ownerId,
                Name: "Test",
                Email: "test@example.com",
                Username: "testuser",
                Role: "Producer",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.AddAsync(A<OwnerSnapshot>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        #endregion

        #region UserCreated - Happy Path

        [Fact]
        public async Task HandleUserCreated_WithNewProducer_ShouldCreateSnapshotAndSave()
        {
            var ct = TestContext.Current.CancellationToken;
            var ownerId = Guid.NewGuid();

            A.CallTo(() => _store.GetByIdAsync(ownerId, A<CancellationToken>._))
                .Returns((OwnerSnapshot?)null);

            var eventData = new UserCreatedIntegrationEvent(
                OwnerId: ownerId,
                Name: "John Doe",
                Email: "john@example.com",
                Username: "johndoe",
                Role: "Producer",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.AddAsync(
                A<OwnerSnapshot>.That.Matches(s =>
                    s.Id == ownerId &&
                    s.Name == "John Doe" &&
                    s.Email == "john@example.com" &&
                    s.IsActive),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleUserCreated_WithProducerRoleCaseInsensitive_ShouldCreateSnapshot()
        {
            var ct = TestContext.Current.CancellationToken;
            var ownerId = Guid.NewGuid();

            A.CallTo(() => _store.GetByIdAsync(ownerId, A<CancellationToken>._))
                .Returns((OwnerSnapshot?)null);

            var eventData = new UserCreatedIntegrationEvent(
                OwnerId: ownerId,
                Name: "Jane",
                Email: "jane@example.com",
                Username: "jane",
                Role: "producer",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _store.AddAsync(A<OwnerSnapshot>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion

        #region UserUpdated - Null Check

        [Fact]
        public async Task HandleUserUpdated_WithNullEvent_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _handler.HandleAsync((EventContext<UserUpdatedIntegrationEvent>)null!,
                    TestContext.Current.CancellationToken));
        }

        #endregion

        #region UserUpdated - Missing Snapshot

        [Fact]
        public async Task HandleUserUpdated_WithMissingSnapshot_ShouldSkip()
        {
            var ct = TestContext.Current.CancellationToken;
            var ownerId = Guid.NewGuid();

            A.CallTo(() => _store.GetByIdAsync(ownerId, A<CancellationToken>._))
                .Returns((OwnerSnapshot?)null);

            var eventData = new UserUpdatedIntegrationEvent(
                OwnerId: ownerId,
                Name: "Updated",
                Email: "updated@example.com",
                Username: "updated",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        #endregion

        #region UserUpdated - Happy Path

        [Fact]
        public async Task HandleUserUpdated_WithExistingSnapshot_ShouldUpdateAndSave()
        {
            var ct = TestContext.Current.CancellationToken;
            var ownerId = Guid.NewGuid();
            var existing = OwnerSnapshot.Create(ownerId, "Old Name", "old@example.com");

            A.CallTo(() => _store.GetByIdAsync(ownerId, A<CancellationToken>._))
                .Returns(existing);

            var eventData = new UserUpdatedIntegrationEvent(
                OwnerId: ownerId,
                Name: "New Name",
                Email: "new@example.com",
                Username: "newuser",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            existing.Name.ShouldBe("New Name");
            existing.Email.ShouldBe("new@example.com");

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion

        #region UserDeactivated - Null Check

        [Fact]
        public async Task HandleUserDeactivated_WithNullEvent_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _handler.HandleAsync((EventContext<UserDeactivatedIntegrationEvent>)null!,
                    TestContext.Current.CancellationToken));
        }

        #endregion

        #region UserDeactivated - Missing Snapshot

        [Fact]
        public async Task HandleUserDeactivated_WithMissingSnapshot_ShouldSkip()
        {
            var ct = TestContext.Current.CancellationToken;
            var ownerId = Guid.NewGuid();

            A.CallTo(() => _store.GetByIdAsync(ownerId, A<CancellationToken>._))
                .Returns((OwnerSnapshot?)null);

            var eventData = new UserDeactivatedIntegrationEvent(
                OwnerId: ownerId,
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        #endregion

        #region UserDeactivated - Happy Path

        [Fact]
        public async Task HandleUserDeactivated_WithExistingSnapshot_ShouldMarkDeletedAndSave()
        {
            var ct = TestContext.Current.CancellationToken;
            var ownerId = Guid.NewGuid();
            var existing = OwnerSnapshot.Create(ownerId, "Name", "email@example.com");

            A.CallTo(() => _store.GetByIdAsync(ownerId, A<CancellationToken>._))
                .Returns(existing);

            var eventData = new UserDeactivatedIntegrationEvent(
                OwnerId: ownerId,
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData), ct);

            existing.IsActive.ShouldBeFalse();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion
    }
}
