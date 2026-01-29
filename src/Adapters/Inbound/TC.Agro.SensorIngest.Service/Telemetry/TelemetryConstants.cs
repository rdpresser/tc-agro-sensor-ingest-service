namespace TC.Agro.SensorIngest.Service.Telemetry
{
    /// <summary>
    /// Constants for telemetry across the application
    /// </summary>
    internal static class TelemetryConstants
    {
        // Versions
        public const string Version = "1.0.0";

        // Service Identity - Centralized for consistency (matches Docker Compose)
        public const string ServiceName = "tcagro-sensor-ingest";
        public const string ServiceNamespace = "tcagro";

        // Meter Names for OpenTelemetry Metrics
        public const string SensorIngestMeterName = "TC.Agro.SensorIngest.Service.Metrics";

        // Activity Source Names for OpenTelemetry Tracing
        public const string SensorIngestActivitySource = "TC.Agro.SensorIngest.Service";
        public const string DatabaseActivitySource = "TC.Agro.SensorIngest.Service.Database";
        public const string CacheActivitySource = "TC.Agro.SensorIngest.Service.Cache";

        // Header Names (standardized)
        public const string CorrelationIdHeader = "x-correlation-id";

        // Tag Names (using underscores for consistency with Loki labels)
        public const string ServiceComponent = "service.component";
        public const string SensorId = "sensor_id";
        public const string SensorAction = "sensor_action";
        public const string ReadingType = "reading_type";
        public const string ErrorType = "error_type";

        // Default Values
        public const string UnknownSensor = "unknown";

        // Service Components
        public const string IngestComponent = "ingest";
        public const string DatabaseComponent = "database";
        public const string CacheComponent = "cache";

        /// <summary>
        /// Logs telemetry configuration details using Microsoft.Extensions.Logging.ILogger
        /// </summary>
        public static void LogTelemetryConfiguration(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration)
        {
            logger.LogInformation("=== TELEMETRY DEBUG INFO ===");
            logger.LogInformation("Service Name: {ServiceName}", ServiceName);
            logger.LogInformation("Service Namespace: {ServiceNamespace}", ServiceNamespace);
            logger.LogInformation("Telemetry Version: {Version}", Version);
            logger.LogInformation("Correlation Header: {CorrelationIdHeader}", CorrelationIdHeader);
            logger.LogInformation("Sensor Ingest Meter: {SensorIngestMeterName}", SensorIngestMeterName);
            logger.LogInformation("Sensor Ingest Activity Source: {SensorIngestActivitySource}", SensorIngestActivitySource);
            logger.LogInformation("Database Activity Source: {DatabaseActivitySource}", DatabaseActivitySource);
            logger.LogInformation("Cache Activity Source: {CacheActivitySource}", CacheActivitySource);
            logger.LogInformation("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NOT SET");
            logger.LogInformation("Machine Name: {MachineName}", Environment.MachineName);
            logger.LogInformation("Container Name: {ContainerName}", Environment.GetEnvironmentVariable("HOSTNAME") ?? "NOT SET");
        }
    }
}
