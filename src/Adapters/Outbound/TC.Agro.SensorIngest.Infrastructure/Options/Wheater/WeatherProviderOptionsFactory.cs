using Microsoft.Extensions.Options;

namespace TC.Agro.SensorIngest.Infrastructure.Options.Wheater
{
    /// <summary>
    /// Factory to access and provide WeatherProviderOptions from dependency injection.
    /// </summary>
    public sealed class WeatherProviderOptionsFactory
    {
        private readonly WeatherProviderOptions _options;

        /// <summary>
        /// Initializes a new instance of the WeatherProviderOptionsFactory class.
        /// </summary>
        /// <param name="options">The weather provider options from DI container</param>
        public WeatherProviderOptionsFactory(IOptions<WeatherProviderOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets the complete weather provider options.
        /// </summary>
        public WeatherProviderOptions Options => _options;

        /// <summary>
        /// Gets weather provider base URL.
        /// </summary>
        public string BaseUrl => _options.BaseUrl;

        /// <summary>
        /// Gets weather provider latitude.
        /// </summary>
        public double Latitude => _options.Latitude;

        /// <summary>
        /// Gets weather provider longitude.
        /// </summary>
        public double Longitude => _options.Longitude;
    }
}
