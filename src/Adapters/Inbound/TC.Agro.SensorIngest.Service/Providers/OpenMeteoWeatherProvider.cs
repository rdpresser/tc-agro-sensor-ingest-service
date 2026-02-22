using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using ZiggyCreatures.Caching.Fusion;

namespace TC.Agro.SensorIngest.Service.Providers
{
    internal sealed class OpenMeteoWeatherProvider : IWeatherDataProvider
    {
        private const string CacheKey = "weather:current";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

        private readonly HttpClient _httpClient;
        private readonly IFusionCache _cache;
        private readonly WeatherProviderOptions _options;
        private readonly ILogger<OpenMeteoWeatherProvider> _logger;

        public OpenMeteoWeatherProvider(
            HttpClient httpClient,
            IFusionCache cache,
            IOptions<WeatherProviderOptions> options,
            ILogger<OpenMeteoWeatherProvider> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<WeatherData?> GetCurrentWeatherAsync(CancellationToken ct = default)
        {
            try
            {
                return await _cache.GetOrSetAsync<WeatherData?>(
                    CacheKey,
                    async (ctx, ct2) => await FetchFromApiAsync(ct2),
                    new FusionCacheEntryOptions { Duration = CacheDuration },
                    ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Open-Meteo API request failed: {StatusCode}", ex.StatusCode);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Open-Meteo API response");
                return null;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Open-Meteo API request timed out");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error fetching weather data");
                return null;
            }
        }

        private async Task<WeatherData?> FetchFromApiAsync(CancellationToken ct)
        {
            var lat = _options.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon = _options.Longitude.ToString(CultureInfo.InvariantCulture);

            var url = $"/v1/forecast?latitude={lat}&longitude={lon}" +
                      "&hourly=temperature_2m,relative_humidity_2m,precipitation,soil_moisture_0_to_1cm" +
                      "&timezone=America/Sao_Paulo&forecast_days=1";

            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            var hourly = doc.RootElement.GetProperty("hourly");
            var times = hourly.GetProperty("time");

            var currentHourIndex = FindCurrentHourIndex(times);
            if (currentHourIndex < 0)
            {
                _logger.LogWarning("Could not find current hour in Open-Meteo response");
                return null;
            }

            var temperature = GetDoubleAt(hourly, "temperature_2m", currentHourIndex);
            var humidity = GetDoubleAt(hourly, "relative_humidity_2m", currentHourIndex);
            var soilMoistureRaw = GetDoubleAt(hourly, "soil_moisture_0_to_1cm", currentHourIndex);
            var precipitation = GetDoubleAt(hourly, "precipitation", currentHourIndex);

            if (temperature is null || humidity is null || soilMoistureRaw is null)
            {
                _logger.LogWarning("Open-Meteo returned null values for essential fields");
                return null;
            }

            var soilMoisture = Math.Round(soilMoistureRaw.Value * 100, 2);
            var precipitationValue = precipitation is > 0 ? precipitation : null;

            _logger.LogInformation(
                "Fetched weather data from Open-Meteo: {Temperature}C, {Humidity}%, SoilMoisture={SoilMoisture}%, Precipitation={Precipitation}mm",
                temperature, humidity, soilMoisture, precipitationValue);

            return new WeatherData(
                Math.Round(temperature.Value, 2),
                Math.Round(humidity.Value, 2),
                soilMoisture,
                precipitationValue.HasValue ? Math.Round(precipitationValue.Value, 2) : null);
        }

        private static int FindCurrentHourIndex(JsonElement times)
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

            var currentHour = now.ToString("yyyy-MM-dd'T'HH:00", CultureInfo.InvariantCulture);

            for (var i = 0; i < times.GetArrayLength(); i++)
            {
                if (times[i].GetString() == currentHour)
                    return i;
            }

            return -1;
        }

        private static double? GetDoubleAt(JsonElement hourly, string property, int index)
        {
            var array = hourly.GetProperty(property);
            var element = array[index];
            return element.ValueKind == JsonValueKind.Null ? null : element.GetDouble();
        }

    }
}
