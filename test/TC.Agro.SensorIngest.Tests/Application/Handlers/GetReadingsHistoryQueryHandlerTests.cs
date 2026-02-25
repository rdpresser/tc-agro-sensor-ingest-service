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
                A<GetReadingsHistoryQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetReadingsHistoryResponse>(), 0));

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = 0 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>.That.Matches(q =>
                    q.SensorId == sensorId &&
                    q.Days == 1 &&
                    q.PageNumber == 1 &&
                    q.PageSize == 10),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WithDaysAboveMaximum_ShouldClampToThirty()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetReadingsHistoryResponse>(), 0));

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = 100 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>.That.Matches(q =>
                    q.SensorId == sensorId &&
                    q.Days == 30),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WithNormalDays_ShouldUseExactValue()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetReadingsHistoryResponse>(), 0));

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = 7 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>.That.Matches(q =>
                    q.SensorId == sensorId &&
                    q.Days == 7),
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
                A<GetReadingsHistoryQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetReadingsHistoryResponse>(), 0));

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = days };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>.That.Matches(q =>
                    q.SensorId == sensorId &&
                    q.Days == 1),
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

            var expectedReadings = new List<GetReadingsHistoryResponse>
            {
                new(Guid.NewGuid(), sensorId, Guid.NewGuid(), DateTimeOffset.UtcNow, 25.0, 60.0, 40.0, 0.0, 85.0)
            };

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>.That.Matches(q => q.SensorId == sensorId),
                A<CancellationToken>._))
                .Returns((expectedReadings, expectedReadings.Count));

            var query = new GetReadingsHistoryQuery { SensorId = sensorId, Days = 7 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Data.Count.ShouldBe(1);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyResult_ShouldReturnEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetReadingsHistoryResponse>(), 0));

            var query = new GetReadingsHistoryQuery { SensorId = Guid.NewGuid(), Days = 7 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Data.ShouldBeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_WithNullResult_ShouldReturnEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetHistoryAsync(
                A<GetReadingsHistoryQuery>._,
                A<CancellationToken>._))
                .Returns(((IReadOnlyList<GetReadingsHistoryResponse>)null!, 0));

            var query = new GetReadingsHistoryQuery { SensorId = Guid.NewGuid(), Days = 7 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Data.ShouldBeEmpty();
        }

        #endregion
    }
}
