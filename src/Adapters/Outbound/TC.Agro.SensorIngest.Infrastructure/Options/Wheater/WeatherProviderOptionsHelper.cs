namespace TC.Agro.SensorIngest.Infrastructure.Options.Wheater
{
    /// <summary>
    /// Helper class to load and build WeatherProviderOptions from configuration.
    /// </summary>
    public sealed class WeatherProviderOptionsHelper
    {
        private const string WeatherProviderSectionName = "WeatherProvider";

        /// <summary>
        /// Gets the weather provider options loaded from configuration.
        /// </summary>
        public WeatherProviderOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the WeatherProviderOptionsHelper class.
        /// Binds configuration section "WeatherProvider" → WeatherProviderOptions.
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        public WeatherProviderOptionsHelper(IConfiguration configuration)
        {
            Options = configuration.GetSection(WeatherProviderSectionName).Get<WeatherProviderOptions>()
                      ?? new WeatherProviderOptions();
        }

        /// <summary>
        /// Static convenience method to get configured weather provider options.
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The configured weather provider options</returns>
        public static WeatherProviderOptions Build(IConfiguration configuration) =>
            new WeatherProviderOptionsHelper(configuration).Options;
    }
}
