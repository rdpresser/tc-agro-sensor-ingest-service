using System.Diagnostics.Metrics;

namespace TC.Agro.SensorIngest.Service.Telemetry
{
    public class SensorIngestMetrics
    {
        private readonly Counter<long> _ingestActionsCounter;
        private readonly Counter<long> _sensorReadingsCounter;
        private readonly Counter<long> _batchReadingsCounter;
        private readonly Counter<long> _alertsCounter;
        private readonly Histogram<double> _operationDurationHistogram;
        private readonly Counter<long> _ingestErrorsCounter;

        public SensorIngestMetrics()
        {
            var meter = new Meter(TelemetryConstants.SensorIngestMeterName, TelemetryConstants.Version);

            _ingestActionsCounter = meter.CreateCounter<long>(
                "ingest_actions_total",
                description: "Total number of ingest-related actions performed by users");

            _sensorReadingsCounter = meter.CreateCounter<long>(
                "sensor_readings_total",
                description: "Total number of sensor readings ingested");

            _batchReadingsCounter = meter.CreateCounter<long>(
                "batch_readings_total",
                description: "Total number of batch reading operations");

            _alertsCounter = meter.CreateCounter<long>(
                "alerts_total",
                description: "Total number of alerts created or resolved");

            _operationDurationHistogram = meter.CreateHistogram<double>(
                "ingest_operation_duration_seconds",
                description: "Duration of ingest operations in seconds");

            _ingestErrorsCounter = meter.CreateCounter<long>(
                "ingest_errors_total",
                description: "Total number of errors in ingest operations");
        }

        public void RecordIngestAction(string action, string userId, string endpoint)
        {
            _ingestActionsCounter.Add(1,
                new KeyValuePair<string, object?>("action", action.ToLowerInvariant()),
                new KeyValuePair<string, object?>("user_id", userId),
                new KeyValuePair<string, object?>("endpoint", endpoint),
                new KeyValuePair<string, object?>("service", "sensor-ingest"));
        }

        public void RecordSensorReading(string operation, string userId, string? sensorId = null, string? plotId = null)
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("operation", operation.ToLowerInvariant()),
                new("user_id", userId),
                new("entity_type", "reading")
            };

            if (!string.IsNullOrWhiteSpace(sensorId))
                tags.Add(new("sensor_id", sensorId));
            if (!string.IsNullOrWhiteSpace(plotId))
                tags.Add(new("plot_id", plotId));

            _sensorReadingsCounter.Add(1, tags.ToArray());
        }

        public void RecordBatchReading(string operation, string userId, int count)
        {
            _batchReadingsCounter.Add(1,
                new KeyValuePair<string, object?>("operation", operation.ToLowerInvariant()),
                new KeyValuePair<string, object?>("user_id", userId),
                new KeyValuePair<string, object?>("batch_size", count));
        }

        public void RecordAlert(string operation, string userId, string? severity = null)
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("operation", operation.ToLowerInvariant()),
                new("user_id", userId),
                new("entity_type", "alert")
            };

            if (!string.IsNullOrWhiteSpace(severity))
                tags.Add(new("severity", severity));

            _alertsCounter.Add(1, tags.ToArray());
        }

        public void RecordOperationDuration(string operation, string entityType, double durationSeconds, bool success = true)
        {
            _operationDurationHistogram.Record(durationSeconds,
                new KeyValuePair<string, object?>("operation", operation.ToLowerInvariant()),
                new KeyValuePair<string, object?>("entity_type", entityType),
                new KeyValuePair<string, object?>("success", success),
                new KeyValuePair<string, object?>("service", "sensor-ingest"));
        }

        public void RecordIngestError(string operation, string entityType, string errorType, string userId)
        {
            _ingestErrorsCounter.Add(1,
                new KeyValuePair<string, object?>("operation", operation.ToLowerInvariant()),
                new KeyValuePair<string, object?>("entity_type", entityType),
                new KeyValuePair<string, object?>("error_type", errorType),
                new KeyValuePair<string, object?>("user_id", userId),
                new KeyValuePair<string, object?>("service", "sensor-ingest"));
        }
    }
}
