using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using TC.Agro.SensorIngest.Application.Abstractions;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;

namespace TC.Agro.SensorIngest.Tests.Application.Handlers
{
    public class GetLatestReadingsQueryHandlerTests
    {
        private readonly ISensorReadingReadStore _readStore;
        private readonly GetLatestReadingsQueryHandler _handler;

        public GetLatestReadingsQueryHandlerTests()
        {
            _readStore = A.Fake<ISensorReadingReadStore>();
            _handler = new GetLatestReadingsQueryHandler(_readStore, NullLogger<GetLatestReadingsQueryHandler>.Instance);
        }

        #region Constructor Validation

        [Fact]
        public void Constructor_WithNullReadStore_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new GetLatestReadingsQueryHandler(null!, NullLogger<GetLatestReadingsQueryHandler>.Instance));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                new GetLatestReadingsQueryHandler(_readStore, null!));
        }

        #endregion

        #region Successful Retrieval

        [Fact]
        public async Task ExecuteAsync_WithValidQuery_ShouldReturnReadings()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            var expectedReadings = new List<GetLatestReadingsResponse>
            {
                new(Guid.NewGuid(), sensorId, Guid.NewGuid(), DateTimeOffset.UtcNow, 25.0, 60.0, 40.0, 0.0, 85.0)
            };

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>.That.Matches(q => q.SensorId == sensorId && q.PageNumber == 1 && q.PageSize == 10),
                A<CancellationToken>._))
                .Returns((expectedReadings, expectedReadings.Count));

            var query = new GetLatestReadingsQuery { SensorId = sensorId, PageSize = 10 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Data.Count.ShouldBe(1);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyResult_ShouldReturnEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetLatestReadingsResponse>(), 0));

            var query = new GetLatestReadingsQuery { PageSize = 10 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Data.ShouldBeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_WithNullResult_ShouldReturnEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>._,
                A<CancellationToken>._))
                .Returns(((IReadOnlyList<GetLatestReadingsResponse>)null!, 0));

            var query = new GetLatestReadingsQuery { PageSize = 10 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Data.ShouldBeEmpty();
        }

        #endregion

        #region PageSize Capping

        [Fact]
        public async Task ExecuteAsync_WithPageSizeAboveMax_ShouldCapAtMaxReadLimit()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetLatestReadingsResponse>(), 0));

            var query = new GetLatestReadingsQuery { PageSize = 5000 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>.That.Matches(q =>
                    q.SensorId == null &&
                    q.PlotId == null &&
                    q.PageNumber == 1 &&
                    q.PageSize == AppConstants.MaxReadLimit),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WithPageSizeBelowMax_ShouldUseProvidedPageSize()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetLatestReadingsResponse>(), 0));

            var query = new GetLatestReadingsQuery { PageSize = 50 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>.That.Matches(q =>
                    q.SensorId == null &&
                    q.PlotId == null &&
                    q.PageNumber == 1 &&
                    q.PageSize == 50),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion

        #region Filter Forwarding

        [Fact]
        public async Task ExecuteAsync_WithSensorIdFilter_ShouldForwardToReadStore()
        {
            var ct = TestContext.Current.CancellationToken;
            var sensorId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetLatestReadingsResponse>(), 0));

            var query = new GetLatestReadingsQuery { SensorId = sensorId, PageSize = 10 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>.That.Matches(q =>
                    q.SensorId == sensorId &&
                    q.PlotId == null &&
                    q.PageSize == 10),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WithPlotIdFilter_ShouldForwardToReadStore()
        {
            var ct = TestContext.Current.CancellationToken;
            var plotId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>._,
                A<CancellationToken>._))
                .Returns((new List<GetLatestReadingsResponse>(), 0));

            var query = new GetLatestReadingsQuery { PlotId = plotId, PageSize = 10 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                A<GetLatestReadingsQuery>.That.Matches(q =>
                    q.SensorId == null &&
                    q.PlotId == plotId &&
                    q.PageSize == 10),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion
    }
}
