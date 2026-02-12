using System.Diagnostics.Metrics;

namespace TC.Agro.SensorIngest.Service.Telemetry
{
    public class SystemMetrics
    {
        private readonly Counter<long> _httpRequestsTotal;
        private readonly Counter<long> _httpErrorsTotal;
        private readonly Histogram<double> _httpRequestDuration;
        private readonly Histogram<double> _databaseQueryDuration;
        private readonly Counter<long> _databaseErrorsTotal;
        private readonly Counter<long> _cacheHits;
        private readonly Counter<long> _cacheMisses;
        private readonly UpDownCounter<long> _activeConnections;

        public SystemMetrics()
        {
            var meter = new Meter(TelemetryConstants.SensorIngestMeterName, TelemetryConstants.Version);

            _httpRequestsTotal = meter.CreateCounter<long>(
                "ingest.http.requests_total",
                description: "Total number of HTTP requests processed");

            _httpErrorsTotal = meter.CreateCounter<long>(
                "ingest.http.errors_total",
                description: "Total number of HTTP errors (4xx, 5xx)");

            _httpRequestDuration = meter.CreateHistogram<double>(
                "ingest.http.request_duration_seconds",
                unit: "s",
                description: "Duration of HTTP requests in seconds");

            _databaseQueryDuration = meter.CreateHistogram<double>(
                "ingest.database.query_duration_seconds",
                unit: "s",
                description: "Duration of database queries in seconds");

            _databaseErrorsTotal = meter.CreateCounter<long>(
                "ingest.database.errors_total",
                description: "Total number of database errors");

            _cacheHits = meter.CreateCounter<long>(
                "ingest.cache.hits_total",
                description: "Total number of cache hits");

            _cacheMisses = meter.CreateCounter<long>(
                "ingest.cache.misses_total",
                description: "Total number of cache misses");

            _activeConnections = meter.CreateUpDownCounter<long>(
                "ingest.connections.active",
                description: "Number of currently active connections");
        }

        public void RecordHttpRequest(string method, string path, int statusCode, double durationSeconds)
        {
            _httpRequestsTotal.Add(1,
                new KeyValuePair<string, object?>("http.method", method),
                new KeyValuePair<string, object?>("http.path", path),
                new KeyValuePair<string, object?>("http.status_code", statusCode.ToString()));

            _httpRequestDuration.Record(durationSeconds,
                new KeyValuePair<string, object?>("http.method", method),
                new KeyValuePair<string, object?>("http.status_code", statusCode.ToString()));

            if (statusCode >= 400)
            {
                var errorCategory = statusCode >= 500 ? "server_error" : "client_error";
                _httpErrorsTotal.Add(1,
                    new KeyValuePair<string, object?>("http.method", method),
                    new KeyValuePair<string, object?>("http.status_code", statusCode.ToString()),
                    new KeyValuePair<string, object?>("error.category", errorCategory));
            }
        }

        public void RecordDatabaseQuery(string operation, double durationSeconds, bool isError = false)
        {
            _databaseQueryDuration.Record(durationSeconds,
                new KeyValuePair<string, object?>("db.operation", operation));

            if (isError)
            {
                _databaseErrorsTotal.Add(1,
                    new KeyValuePair<string, object?>("db.operation", operation));
            }
        }

        public void RecordCacheHit(string cacheKey)
        {
            _cacheHits.Add(1,
                new KeyValuePair<string, object?>("cache.key", cacheKey));
        }

        public void RecordCacheMiss(string cacheKey)
        {
            _cacheMisses.Add(1,
                new KeyValuePair<string, object?>("cache.key", cacheKey));
        }

        public void ConnectionOpened() => _activeConnections.Add(1);
        public void ConnectionClosed() => _activeConnections.Add(-1);
    }
}
