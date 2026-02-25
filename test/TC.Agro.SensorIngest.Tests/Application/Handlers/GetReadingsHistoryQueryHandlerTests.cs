using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory;

namespace TC.Agro.SensorIngest.Tests.Application.Handlers
{
    public class GetReadingsHistoryQueryHandlerTests
    {
        private readonly ISensorReadingReadStore _readStore;
        private readonly GetReadingsHistoryQueryHandler _handler;

        public GetReadingsHistoryQueryHandlerTests()
        {
            _readStore = A.Fake<ISensorReadingReadStore>();
            _handler = new GetReadingsHistoryQueryHandler(_readStore, NullLogger<GetReadingsHistoryQueryHandler>.Instance);
        }

        #region Constructor Validation

        [Fact]
        public void Constructor_WithNullReadStore_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new GetReadingsHistoryQueryHandler(null!, NullLogger<GetReadingsHistoryQueryHandler>.Instance));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new GetReadingsHistoryQueryHandler(_readStore, null!));
        }

        #endregion

        #region Days Clamping

        [Fact]
        public async Task ExecuteAsync_WithDaysBelowMinimum_ShouldClampToOne()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<Guid>._, A<DateTime>._, A<DateTime>._, A<CancellationToken>._))
                .Returns(Enumerable.Empty<ReadingHistoryItem>());

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = 0 };

            await _handler.ExecuteAsync(query, ct);

            // With days clamped to 1, the 'from' date should be ~1 day ago
            A.CallTo(() => _readStore.GetHistoryAsync(
                sensorId,
                A<DateTime>.That.Matches(d => (DateTime.UtcNow - d).TotalDays < 1.1 && (DateTime.UtcNow - d).TotalDays > 0.9),
                A<DateTime>._,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WithDaysAboveMaximum_ShouldClampToThirty()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<Guid>._, A<DateTime>._, A<DateTime>._, A<CancellationToken>._))
                .Returns(Enumerable.Empty<ReadingHistoryItem>());

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = 100 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetHistoryAsync(
                sensorId,
                A<DateTime>.That.Matches(d => (DateTime.UtcNow - d).TotalDays < 30.1 && (DateTime.UtcNow - d).TotalDays > 29.9),
                A<DateTime>._,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WithNormalDays_ShouldUseExactValue()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<Guid>._, A<DateTime>._, A<DateTime>._, A<CancellationToken>._))
                .Returns(Enumerable.Empty<ReadingHistoryItem>());

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = 7 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetHistoryAsync(
                sensorId,
                A<DateTime>.That.Matches(d => (DateTime.UtcNow - d).TotalDays < 7.1 && (DateTime.UtcNow - d).TotalDays > 6.9),
                A<DateTime>._,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(-1)]
        public async Task ExecuteAsync_WithNegativeDays_ShouldClampToOne(int days)
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<Guid>._, A<DateTime>._, A<DateTime>._, A<CancellationToken>._))
                .Returns(Enumerable.Empty<ReadingHistoryItem>());

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = days };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetHistoryAsync(
                sensorId,
                A<DateTime>.That.Matches(d => (DateTime.UtcNow - d).TotalDays < 1.1 && (DateTime.UtcNow - d).TotalDays > 0.9),
                A<DateTime>._,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion

        #region Successful Retrieval

        [Fact]
        public async Task ExecuteAsync_WithValidQuery_ShouldReturnReadings()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            var expectedReadings = new List<ReadingHistoryItem>
            {
                new(Guid.NewGuid(), sensorId, Guid.NewGuid(), DateTime.UtcNow, 25.0, 60.0, 40.0, 0.0, 85.0)
            };

            A.CallTo(() => _readStore.GetHistoryAsync(
                sensorId, A<DateTime>._, A<DateTime>._, A<CancellationToken>._))
                .Returns(expectedReadings);

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = 7 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Readings.Count.ShouldBe(1);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyResult_ShouldReturnEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<Guid>._, A<DateTime>._, A<DateTime>._, A<CancellationToken>._))
                .Returns(Enumerable.Empty<ReadingHistoryItem>());

            var query = new GetReadingsHistoryQuery { SensorId = Guid.NewGuid(), Days = 7 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Readings.ShouldBeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_WithNullResult_ShouldReturnEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<Guid>._, A<DateTime>._, A<DateTime>._, A<CancellationToken>._))
                .Returns((IEnumerable<ReadingHistoryItem>)null!);

            var query = new GetReadingsHistoryQuery { SensorId = Guid.NewGuid(), Days = 7 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Readings.ShouldBeEmpty();
        }

        #endregion
    }
}
