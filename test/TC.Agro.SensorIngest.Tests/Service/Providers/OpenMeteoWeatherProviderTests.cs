using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Service.Providers;
using ZiggyCreatures.Caching.Fusion;

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

        private OpenMeteoWeatherProvider CreateProvider(string responseJson, HttpStatusCode statusCode)
        {
            var handler = new FakeHttpMessageHandler(responseJson, statusCode);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.open-meteo.com")
            };

            var cache = new FusionCache(new FusionCacheOptions());

            return new OpenMeteoWeatherProvider(
                httpClient,
                cache,
                _options,
                NullLogger<OpenMeteoWeatherProvider>.Instance);
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

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _response;
            private readonly HttpStatusCode _statusCode;

            public FakeHttpMessageHandler(string response, HttpStatusCode statusCode)
            {
                _response = response;
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_response, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
