using System.Diagnostics;

namespace TC.Agro.SensorIngest.Service.Telemetry
{
    internal static class ActivitySourceFactory
    {
        public static readonly ActivitySource Handlers =
            new(TelemetryConstants.HandlersActivitySource, TelemetryConstants.Version);

        public static readonly ActivitySource FastEndpoints =
            new(TelemetryConstants.FastEndpointsActivitySource, TelemetryConstants.Version);

        public static readonly ActivitySource Database =
            new(TelemetryConstants.DatabaseActivitySource, TelemetryConstants.Version);

        public static readonly ActivitySource Cache =
            new(TelemetryConstants.CacheActivitySource, TelemetryConstants.Version);

        public static Activity? StartHandlerOperation(string operationName, string userId = TelemetryConstants.AnonymousUser)
        {
            var activity = Handlers.StartActivity(operationName);
            activity?.SetTag("handler.name", operationName);
            activity?.SetTag("user.id", userId);
            return activity;
        }

        public static Activity? StartDatabaseOperation(string operation, string tableName)
        {
            var activity = Database.StartActivity(operation);
            activity?.SetTag("db.operation", operation);
            activity?.SetTag("db.table", tableName);
            return activity;
        }

        public static Activity? StartCacheOperation(string operation, string cacheKey)
        {
            var activity = Cache.StartActivity(operation);
            activity?.SetTag("cache.operation", operation);
            activity?.SetTag("cache.key", cacheKey);
            return activity;
        }

        public static Activity? StartIngestOperation(string operationName, string entityType, string? entityId = null, string userId = TelemetryConstants.AnonymousUser)
        {
            var activity = Handlers.StartActivity(operationName);
            activity?.SetTag("ingest.operation", operationName);
            activity?.SetTag("ingest.entity_type", entityType);
            activity?.SetTag("user.id", userId);

            if (!string.IsNullOrWhiteSpace(entityId))
            {
                activity?.SetTag("ingest.entity_id", entityId);
            }

            return activity;
        }

        public static Activity? StartEndpointOperation(string endpointName, string userId = TelemetryConstants.AnonymousUser)
        {
            var activity = FastEndpoints.StartActivity(endpointName);
            activity?.SetTag("endpoint.name", endpointName);
            activity?.SetTag("user.id", userId);
            return activity;
        }
    }
}
