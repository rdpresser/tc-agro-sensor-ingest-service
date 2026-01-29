namespace TC.Agro.SensorIngest.Application.Abstractions
{
    public static class AppConstants
    {
        public const int MaxBatchSize = 1000;
        public const int DefaultReadLimit = 100;
        public const int MaxReadLimit = 1000;
        public const int CacheTtlSeconds = 60;

        public static class CacheKeys
        {
            public const string LatestReadingsPrefix = "sensor:latest:";
            public const string AggregatesPrefix = "sensor:aggregates:";
        }
    }
}
