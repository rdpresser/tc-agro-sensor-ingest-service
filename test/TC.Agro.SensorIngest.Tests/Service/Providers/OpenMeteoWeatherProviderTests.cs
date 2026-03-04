using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Infrastructure.Options.Wheater;
using TC.Agro.SensorIngest.Service.Providers;
using TC.Agro.SharedKernel.Infrastructure.Caching.Service;

namespace TC.Agro.SensorIngest.Tests.Service.Providers
{
    public class OpenMeteoWeatherProviderTests
    {
        private readonly IOptions<WeatherProviderOptions> _options;

        public OpenMeteoWeatherProviderTests()
        {
            _options = Options.Create(new WeatherProviderOptions
            {
                Latitude = -22.7256,
                Longitude = -47.6492
            });
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_WithValidResponse_ShouldReturnWeatherData()
        {
            var currentHour = GetCurrentSaoPauloHour();
            var json = BuildOpenMeteoJson(currentHour, 28.5, 65, 0.30, 2.5);
            var provider = CreateProvider(json, HttpStatusCode.OK);

            var result = await provider.GetCurrentWeatherAsync(TestContext.Current.CancellationToken);

            result.ShouldNotBeNull();
            result.Temperature.ShouldBe(28.5);
            result.Humidity.ShouldBe(65);
            result.SoilMoisture.ShouldBe(30.0);
            result.Precipitation.ShouldBe(2.5);
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_WithZeroPrecipitation_ShouldReturnNullPrecipitation()
        {
            var currentHour = GetCurrentSaoPauloHour();
            var json = BuildOpenMeteoJson(currentHour, 25.0, 60, 0.25, 0.0);
            var provider = CreateProvider(json, HttpStatusCode.OK);

            var result = await provider.GetCurrentWeatherAsync(TestContext.Current.CancellationToken);

            result.ShouldNotBeNull();
            result.Precipitation.ShouldBeNull();
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_WithHttpError_ShouldReturnNull()
        {
            var provider = CreateProvider("", HttpStatusCode.InternalServerError);

            var result = await provider.GetCurrentWeatherAsync(TestContext.Current.CancellationToken);

            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_WithInvalidJson_ShouldReturnNull()
        {
            var provider = CreateProvider("{ invalid json !!!", HttpStatusCode.OK);

            var result = await provider.GetCurrentWeatherAsync(TestContext.Current.CancellationToken);

            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_WithMissingCurrentHour_ShouldReturnNull()
        {
            var json = BuildOpenMeteoJson("2020-01-01T00:00", 25.0, 60, 0.25, 0.0);
            var provider = CreateProvider(json, HttpStatusCode.OK);

            var result = await provider.GetCurrentWeatherAsync(TestContext.Current.CancellationToken);

            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_SoilMoistureConversion_ShouldMultiplyBy100()
        {
            var currentHour = GetCurrentSaoPauloHour();
            var json = BuildOpenMeteoJson(currentHour, 20.0, 50, 0.42, 0.0);
            var provider = CreateProvider(json, HttpStatusCode.OK);

            var result = await provider.GetCurrentWeatherAsync(TestContext.Current.CancellationToken);

            result.ShouldNotBeNull();
            result.SoilMoisture.ShouldBe(42.0);
        }

        [Fact]
        public async Task GetCurrentWeatherBatchAsync_WithMultipleLocations_ShouldReturnWeatherForEachLocation()
        {
            var currentHour = GetCurrentSaoPauloHour();
            var json = BuildOpenMeteoBatchJson(
                currentHour,
                (28.5, 65, 0.30, 2.5),
                (25.0, 60, 0.25, 0.0));

            var provider = CreateProvider(json, HttpStatusCode.OK);
            var locations = new List<WeatherLocation>
            {
                new(-22.7256, -47.6492),
                new(-22.9000, -47.1000)
            };

            var result = await provider.GetCurrentWeatherBatchAsync(locations, TestContext.Current.CancellationToken);

            result.Count.ShouldBe(2);
            result[locations[0]].Temperature.ShouldBe(28.5);
            result[locations[0]].Humidity.ShouldBe(65);
            result[locations[0]].SoilMoisture.ShouldBe(30.0);
            result[locations[0]].Precipitation.ShouldBe(2.5);

            result[locations[1]].Temperature.ShouldBe(25.0);
            result[locations[1]].Humidity.ShouldBe(60);
            result[locations[1]].SoilMoisture.ShouldBe(25.0);
            result[locations[1]].Precipitation.ShouldBeNull();
        }

        [Fact]
        public async Task GetCurrentWeatherBatchAsync_WithDuplicateLocations_ShouldCallApiOnlyOnce()
        {
            var currentHour = GetCurrentSaoPauloHour();
            var json = BuildOpenMeteoJson(currentHour, 26.0, 55, 0.28, 1.5);
            var (provider, handler) = CreateProviderWithHandler(json, HttpStatusCode.OK);

            var location = new WeatherLocation(-22.7256, -47.6492);
            var locations = new List<WeatherLocation> { location, location, location };

            var result = await provider.GetCurrentWeatherBatchAsync(locations, TestContext.Current.CancellationToken);

            result.Count.ShouldBe(1);
            handler.CallCount.ShouldBe(1);
        }

        [Fact]
        public async Task GetCurrentWeatherBatchAsync_WithEmptyLocations_ShouldReturnEmptyWithoutCallingApi()
        {
            var (provider, handler) = CreateProviderWithHandler("", HttpStatusCode.OK);

            var result = await provider.GetCurrentWeatherBatchAsync([], TestContext.Current.CancellationToken);

            result.ShouldBeEmpty();
            handler.CallCount.ShouldBe(0);
        }

        private OpenMeteoWeatherProvider CreateProvider(string responseJson, HttpStatusCode statusCode)
        {
            var (provider, _) = CreateProviderWithHandler(responseJson, statusCode);
            return provider;
        }

        private (OpenMeteoWeatherProvider Provider, FakeHttpMessageHandler Handler) CreateProviderWithHandler(
            string responseJson,
            HttpStatusCode statusCode)
        {
            var handler = new FakeHttpMessageHandler(responseJson, statusCode);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.open-meteo.com")
            };

            var cache = new FakeCacheService();

            var provider = new OpenMeteoWeatherProvider(
                httpClient,
                cache,
                _options,
                NullLogger<OpenMeteoWeatherProvider>.Instance);

            return (provider, handler);
        }

        private static string GetCurrentSaoPauloHour()
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));
            return now.ToString("yyyy-MM-dd'T'HH:00", CultureInfo.InvariantCulture);
        }

        private static string BuildOpenMeteoJson(
            string timeEntry, double temperature, double humidity,
            double soilMoisture, double precipitation)
        {
            return $$"""
            {
                "hourly": {
                    "time": ["{{timeEntry}}"],
                    "temperature_2m": [{{temperature.ToString(CultureInfo.InvariantCulture)}}],
                    "relative_humidity_2m": [{{humidity.ToString(CultureInfo.InvariantCulture)}}],
                    "soil_moisture_0_to_1cm": [{{soilMoisture.ToString(CultureInfo.InvariantCulture)}}],
                    "precipitation": [{{precipitation.ToString(CultureInfo.InvariantCulture)}}]
                }
            }
            """;
        }

        private static string BuildOpenMeteoBatchJson(
            string timeEntry,
            params (double Temperature, double Humidity, double SoilMoisture, double Precipitation)[] entries)
        {
            var payloads = entries
                .Select((entry, index) => $$"""
                {
                    "location_id": {{index}},
                    "hourly": {
                        "time": ["{{timeEntry}}"],
                        "temperature_2m": [{{entry.Temperature.ToString(CultureInfo.InvariantCulture)}}],
                        "relative_humidity_2m": [{{entry.Humidity.ToString(CultureInfo.InvariantCulture)}}],
                        "soil_moisture_0_to_1cm": [{{entry.SoilMoisture.ToString(CultureInfo.InvariantCulture)}}],
                        "precipitation": [{{entry.Precipitation.ToString(CultureInfo.InvariantCulture)}}]
                    }
                }
                """);

            return $"[{string.Join(",", payloads)}]";
        }

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _response;
            private readonly HttpStatusCode _statusCode;

            public int CallCount { get; private set; }
            public Uri? LastRequestUri { get; private set; }

            public FakeHttpMessageHandler(string response, HttpStatusCode statusCode)
            {
                _response = response;
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                LastRequestUri = request.RequestUri;

                return Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_response, Encoding.UTF8, "application/json")
                });
            }
        }

        private sealed class FakeCacheService : ICacheService
        {
            public Task<T?> GetAsync<T>(
                string key,
                TimeSpan? duration = null,
                TimeSpan? distributedCacheDuration = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(default(T));
            }

            public async Task<T?> GetOrSetAsync<T>(
                string key,
                Func<CancellationToken, Task<T>> factory,
                TimeSpan? duration = null,
                TimeSpan? distributedCacheDuration = null,
                CancellationToken cancellationToken = default)
            {
                return await factory(cancellationToken).ConfigureAwait(false);
            }

            public Task SetAsync<T>(
                string key,
                T value,
                TimeSpan? duration = null,
                TimeSpan? distributedCacheDuration = null,
                IReadOnlyCollection<string>? tags = null,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task RemoveAsync(
                string key,
                TimeSpan? duration = null,
                TimeSpan? distributedCacheDuration = null,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task RemoveByTagAsync(
                string tag,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task RemoveByTagAsync(
                IEnumerable<string> tags,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
