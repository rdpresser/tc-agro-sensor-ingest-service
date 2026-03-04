namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface IWeatherDataProvider
    {
        Task<WeatherData?> GetCurrentWeatherAsync(CancellationToken ct = default);

        Task<IReadOnlyDictionary<WeatherLocation, WeatherData>> GetCurrentWeatherBatchAsync(
            IReadOnlyCollection<WeatherLocation> locations,
            CancellationToken ct = default);
    }

    public sealed record WeatherLocation(
        double Latitude,
        double Longitude);

    public sealed record WeatherData(
        double Temperature,
        double Humidity,
        double SoilMoisture,
        double? Precipitation);
}
