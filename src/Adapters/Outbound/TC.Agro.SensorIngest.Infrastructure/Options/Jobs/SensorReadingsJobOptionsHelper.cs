namespace TC.Agro.SensorIngest.Infrastructure.Options.Jobs
{
    /// <summary>
    /// Helper class to load and build SensorReadingsJobOptions from configuration.
    /// Follows the same pattern as RabbitMqHelper for consistency.
    /// </summary>
    public sealed class SensorReadingsJobOptionsHelper
    {
        private const string SensorReadingsJobSectionName = "Jobs:SensorReadings";

        /// <summary>
        /// Gets the sensor readings job options loaded from configuration.
        /// </summary>
        public SensorReadingsJobOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the SensorReadingsJobOptionsHelper class.
        /// Binds configuration section "Jobs:SensorReadings" → SensorReadingsJobOptions
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        public SensorReadingsJobOptionsHelper(IConfiguration configuration)
        {
            // Bind section "Jobs:SensorReadings" → SensorReadingsJobOptions
            Options = configuration.GetSection(SensorReadingsJobSectionName).Get<SensorReadingsJobOptions>()
                      ?? new SensorReadingsJobOptions();
        }

        /// <summary>
        /// Static convenience method to get configured sensor readings job options.
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The configured sensor readings job options</returns>
        public static SensorReadingsJobOptions Build(IConfiguration configuration) =>
            new SensorReadingsJobOptionsHelper(configuration).Options;
    }
}
