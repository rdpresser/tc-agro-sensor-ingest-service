using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TC.Agro.SensorIngest.Application.Abstractions.Ports;
using TC.Agro.SensorIngest.Infrastructure.Options.Wheater;
using TC.Agro.SharedKernel.Infrastructure.Caching.Service;

namespace TC.Agro.SensorIngest.Service.Providers
{
    internal sealed class OpenMeteoWeatherProvider : IWeatherDataProvider
    {
        private const string CacheKeyPrefix = "weather:current";
        private const string RequestTimeZone = "America/Sao_Paulo";
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(60);

        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly WeatherProviderOptions _options;
        private readonly ILogger<OpenMeteoWeatherProvider> _logger;

        public OpenMeteoWeatherProvider(
            HttpClient httpClient,
            ICacheService cacheService,
            IOptions<WeatherProviderOptions> options,
            ILogger<OpenMeteoWeatherProvider> logger)
        {
            _httpClient = httpClient;
            _cacheService = cacheService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<WeatherData?> GetCurrentWeatherAsync(CancellationToken ct = default)
        {
            var defaultLocation = new WeatherLocation(_options.Latitude, _options.Longitude);
            var weatherByLocation = await GetCurrentWeatherBatchAsync([defaultLocation], ct).ConfigureAwait(false);

            return weatherByLocation.TryGetValue(defaultLocation, out var weatherData)
                ? weatherData
                : null;
        }

        public async Task<IReadOnlyDictionary<WeatherLocation, WeatherData>> GetCurrentWeatherBatchAsync(
            IReadOnlyCollection<WeatherLocation> locations,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(locations);

            var validLocations = locations
                .Where(IsValidLocation)
                .Distinct()
                .ToList();

            if (validLocations.Count == 0)
                return new Dictionary<WeatherLocation, WeatherData>();

            try
            {
                var weatherByLocation = new Dictionary<WeatherLocation, WeatherData>();
                var missingLocations = new List<WeatherLocation>();

                foreach (var location in validLocations)
                {
                    var cachedWeather = await _cacheService.GetAsync<WeatherData>(
                        BuildCacheKey(location),
                        duration: _cacheDuration,
                        distributedCacheDuration: _cacheDuration,
                        cancellationToken: ct).ConfigureAwait(false);

                    if (cachedWeather is not null)
                    {
                        weatherByLocation[location] = cachedWeather;
                    }
                    else
                    {
                        missingLocations.Add(location);
                    }
                }

                if (missingLocations.Count == 0)
                    return weatherByLocation;

                _logger.LogInformation(
                    "Fetching weather data for {Count} unique location(s) from Open-Meteo",
                    missingLocations.Count);

                foreach (var locationBatch in missingLocations.Chunk(_options.MaxCoordinatesPerRequest))
                {
                    var fetchedWeather = await FetchFromApiAsync(locationBatch, ct).ConfigureAwait(false);

                    foreach (var weather in fetchedWeather)
                    {
                        weatherByLocation[weather.Key] = weather.Value;

                        await _cacheService.SetAsync(
                            BuildCacheKey(weather.Key),
                            weather.Value,
                            duration: _cacheDuration,
                            distributedCacheDuration: _cacheDuration,
                            cancellationToken: ct).ConfigureAwait(false);
                    }
                }

                return weatherByLocation;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Open-Meteo API request failed: {StatusCode}", ex.StatusCode);
                return new Dictionary<WeatherLocation, WeatherData>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Open-Meteo API response");
                return new Dictionary<WeatherLocation, WeatherData>();
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Open-Meteo API request timed out");
                return new Dictionary<WeatherLocation, WeatherData>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error fetching weather data");
                return new Dictionary<WeatherLocation, WeatherData>();
            }
        }

        private async Task<IReadOnlyDictionary<WeatherLocation, WeatherData>> FetchFromApiAsync(
            IReadOnlyList<WeatherLocation> locations,
            CancellationToken ct)
        {
            var latitudes = string.Join(",", locations.Select(location => location.Latitude.ToString(CultureInfo.InvariantCulture)));
            var longitudes = string.Join(",", locations.Select(location => location.Longitude.ToString(CultureInfo.InvariantCulture)));
            var encodedTimezone = Uri.EscapeDataString(RequestTimeZone);

            var url = $"/v1/forecast?latitude={latitudes}&longitude={longitudes}" +
                      "&hourly=temperature_2m,relative_humidity_2m,precipitation,soil_moisture_0_to_1cm" +
                      $"&timezone={encodedTimezone}&forecast_days=1";

            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            return ParseWeatherResponse(doc.RootElement, locations);
        }

        private IReadOnlyDictionary<WeatherLocation, WeatherData> ParseWeatherResponse(
            JsonElement rootElement,
            IReadOnlyList<WeatherLocation> requestedLocations)
        {
            var weatherByLocation = new Dictionary<WeatherLocation, WeatherData>();

            if (rootElement.ValueKind == JsonValueKind.Object)
            {
                if (requestedLocations.Count == 0)
                    return weatherByLocation;

                TryAddWeatherData(weatherByLocation, requestedLocations[0], rootElement);
                return weatherByLocation;
            }

            if (rootElement.ValueKind == JsonValueKind.Array)
            {
                for (var i = 0; i < rootElement.GetArrayLength(); i++)
                {
                    var payload = rootElement[i];
                    var locationIndex = ResolveLocationIndex(payload, i);

                    if (locationIndex < 0 || locationIndex >= requestedLocations.Count)
                        continue;

                    var location = requestedLocations[locationIndex];
                    TryAddWeatherData(weatherByLocation, location, payload);
                }

                return weatherByLocation;
            }

            _logger.LogWarning(
                "Unexpected Open-Meteo payload kind {PayloadKind} while parsing weather response",
                rootElement.ValueKind);

            return weatherByLocation;
        }

        private void TryAddWeatherData(
            IDictionary<WeatherLocation, WeatherData> weatherByLocation,
            WeatherLocation location,
            JsonElement payload)
        {
            var weatherData = ParseWeatherData(payload);
            if (weatherData is not null)
            {
                weatherByLocation[location] = weatherData;
            }
        }

        private WeatherData? ParseWeatherData(JsonElement payload)
        {
            if (!payload.TryGetProperty("hourly", out var hourly))
            {
                _logger.LogWarning("Open-Meteo payload does not contain 'hourly' data");
                return null;
            }

            if (!hourly.TryGetProperty("time", out var times))
            {
                _logger.LogWarning("Open-Meteo payload does not contain 'hourly.time' data");
                return null;
            }

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

        private static int ResolveLocationIndex(JsonElement payload, int fallbackIndex)
        {
            if (payload.TryGetProperty("location_id", out var locationIdProperty)
                && locationIdProperty.ValueKind == JsonValueKind.Number
                && locationIdProperty.TryGetInt32(out var locationId))
            {
                return locationId;
            }

            return fallbackIndex;
        }

        private static int FindCurrentHourIndex(JsonElement times)
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById(RequestTimeZone));

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

        private static bool IsValidLocation(WeatherLocation location)
        {
            return location.Latitude is >= -90 and <= 90
                && location.Longitude is >= -180 and <= 180;
        }

        private static string BuildCacheKey(WeatherLocation location)
        {
            var lat = location.Latitude.ToString("R", CultureInfo.InvariantCulture);
            var lon = location.Longitude.ToString("R", CultureInfo.InvariantCulture);
            return $"{CacheKeyPrefix}:{lat}:{lon}";
        }

    }
}
