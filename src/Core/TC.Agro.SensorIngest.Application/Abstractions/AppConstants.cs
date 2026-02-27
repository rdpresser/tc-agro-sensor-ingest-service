using System.Collections.Frozen;

namespace TC.Agro.SensorIngest.Application.Abstractions
{
    public static class AppConstants
    {
        public const int MaxBatchSize = 1000;
        public const int DefaultReadLimit = 100;
        public const int MaxReadLimit = 1000;

        /// <summary>
        /// Sensor status constants and validation.
        /// Access like: AppConstants.ValidStatuses.Active
        /// Validate with: AppConstants.ValidStatuses.All.Contains(status)
        /// Keep these in sync with the SensorStatus in the ValueObjects from farm domain, and with the allowed values in the database schema.
        /// </summary>
        public static class ValidStatuses
        {
            public const string Active = "Active";
            public const string Inactive = "Inactive";
            public const string Maintenance = "Maintenance";
            public const string Faulty = "Faulty";

            /// <summary>
            /// Frozen set containing all valid sensor statuses (case-insensitive).
            /// </summary>
            public static readonly FrozenSet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Active,
                Inactive,
                Maintenance,
                Faulty
            }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Validates if a status string is valid (case-insensitive).
            /// </summary>
            /// <param name="status">The status to validate</param>
            /// <returns>True if the status is valid, false otherwise</returns>
            public static bool IsValid(string? status) =>
                !string.IsNullOrWhiteSpace(status) && All.Contains(status);
        }
    }
}
