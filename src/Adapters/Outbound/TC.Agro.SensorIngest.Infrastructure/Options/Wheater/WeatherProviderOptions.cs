namespace TC.Agro.SensorIngest.Infrastructure.Options.Wheater
{
    public sealed class WeatherProviderOptions
    {
        public string BaseUrl { get; set; } = "https://api.open-meteo.com";
        public double Latitude { get; set; } = -22.7256;
        public double Longitude { get; set; } = -47.6492;
    }
}
