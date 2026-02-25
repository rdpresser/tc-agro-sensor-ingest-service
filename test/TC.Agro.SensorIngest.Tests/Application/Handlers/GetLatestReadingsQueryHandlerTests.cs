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

            var expectedReadings = new List<LatestReadingItem>
            {
                new(Guid.NewGuid(), sensorId, Guid.NewGuid(), DateTime.UtcNow, 25.0, 60.0, 40.0, 0.0, 85.0)
            };

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: sensorId,
                plotId: null,
                limit: 10,
                cancellationToken: A<CancellationToken>._))
                .Returns(expectedReadings);

            var query = new GetLatestReadingsQuery { SensorId = sensorId, Limit = 10 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Readings.Count.ShouldBe(1);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyResult_ShouldReturnEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: A<Guid?>._, 
                plotId: A<Guid?>._, 
                limit: A<int>._, 
                cancellationToken: A<CancellationToken>._))
                .Returns(new List<LatestReadingItem>());

            var query = new GetLatestReadingsQuery { Limit = 10 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Readings.ShouldBeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_WithNullResult_ShouldReturnEmptyList()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: A<Guid?>._, 
                plotId: A<Guid?>._, 
                limit: A<int>._, 
                cancellationToken: A<CancellationToken>._))
                .Returns((IReadOnlyList<LatestReadingItem>)null!);

            var query = new GetLatestReadingsQuery { Limit = 10 };

            var result = await _handler.ExecuteAsync(query, ct);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Readings.ShouldBeEmpty();
        }

        #endregion

        #region Limit Capping

        [Fact]
        public async Task ExecuteAsync_WithLimitAboveMax_ShouldCapAtMaxReadLimit()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: A<Guid?>._, 
                plotId: A<Guid?>._, 
                limit: A<int>._, 
                cancellationToken: A<CancellationToken>._))
                .Returns(new List<LatestReadingItem>());

            var query = new GetLatestReadingsQuery { Limit = 5000 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: null, 
                plotId: null, 
                limit: AppConstants.MaxReadLimit, 
                cancellationToken: A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WithLimitBelowMax_ShouldUseProvidedLimit()
        {
            var ct = TestContext.Current.CancellationToken;

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: A<Guid?>._, 
                plotId: A<Guid?>._, 
                limit: A<int>._, 
                cancellationToken: A<CancellationToken>._))
                .Returns(new List<LatestReadingItem>());

            var query = new GetLatestReadingsQuery { Limit = 50 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: null, 
                plotId: null, 
                limit: 50, 
                cancellationToken: A<CancellationToken>._))
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
                sensorId: A<Guid?>._, 
                plotId: A<Guid?>._, 
                limit: A<int>._, 
                cancellationToken: A<CancellationToken>._))
                .Returns(new List<LatestReadingItem>());

            var query = new GetLatestReadingsQuery { SensorId = sensorId, Limit = 10 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: sensorId, 
                plotId: null, 
                limit: 10, 
                cancellationToken: A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WithPlotIdFilter_ShouldForwardToReadStore()
        {
            var ct = TestContext.Current.CancellationToken;
            var plotId = Guid.NewGuid();

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: A<Guid?>._, 
                plotId: A<Guid?>._, 
                limit: A<int>._, 
                cancellationToken: A<CancellationToken>._))
                .Returns(new List<LatestReadingItem>());

            var query = new GetLatestReadingsQuery { PlotId = plotId, Limit = 10 };

            await _handler.ExecuteAsync(query, ct);

            A.CallTo(() => _readStore.GetLatestReadingsAsync(
                sensorId: null, 
                plotId: plotId, 
                limit: 10, 
                cancellationToken: A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        #endregion
    }
}
