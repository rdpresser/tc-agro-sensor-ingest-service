namespace TC.Agro.SensorIngest.Application.Abstractions.Ports
{
    public interface IWeatherDataProvider
    {
        Task<WeatherData?> GetCurrentWeatherAsync(CancellationToken ct = default);
    }

    public sealed record WeatherData(
        double Temperature,
        double Humidity,
        double SoilMoisture,
        double? Precipitation);
}
