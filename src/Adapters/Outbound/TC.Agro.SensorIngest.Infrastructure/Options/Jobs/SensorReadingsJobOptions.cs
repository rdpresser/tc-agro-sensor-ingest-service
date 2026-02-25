namespace TC.Agro.SensorIngest.Infrastructure.Options.Jobs
{
    /// <summary>
    /// Configuration options for the simulated sensor readings job.
    /// Bind from "Jobs:SensorReadings" section in appsettings.json.
    /// </summary>
    public sealed class SensorReadingsJobOptions
    {
        /// <summary>
        /// Gets or sets whether the sensor readings job is enabled.
        /// Default: false
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the interval in seconds between job executions.
        /// Default: 5 seconds
        /// Must be greater than 0.
        /// </summary>
        public int IntervalSeconds { get; set; } = 5;
    }
}
