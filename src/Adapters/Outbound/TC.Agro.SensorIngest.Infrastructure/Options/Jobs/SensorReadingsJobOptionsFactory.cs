using Microsoft.Extensions.Options;

namespace TC.Agro.SensorIngest.Infrastructure.Options.Jobs
{
    /// <summary>
    /// Factory to access and provide SensorReadingsJobOptions from dependency injection.
    /// Follows the same pattern as RabbitMqConnectionFactory for consistency.
    /// </summary>
    public sealed class SensorReadingsJobOptionsFactory
    {
        private readonly SensorReadingsJobOptions _options;

        /// <summary>
        /// Initializes a new instance of the SensorReadingsJobOptionsFactory class.
        /// </summary>
        /// <param name="options">The sensor readings job options from DI container</param>
        public SensorReadingsJobOptionsFactory(IOptions<SensorReadingsJobOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets the complete sensor readings job options.
        /// </summary>
        public SensorReadingsJobOptions Options => _options;

        /// <summary>
        /// Gets whether the sensor readings job is enabled.
        /// </summary>
        public bool IsEnabled => _options.Enabled;

        /// <summary>
        /// Gets the interval in seconds between job executions.
        /// </summary>
        public int IntervalSeconds => _options.IntervalSeconds;
    }
}
