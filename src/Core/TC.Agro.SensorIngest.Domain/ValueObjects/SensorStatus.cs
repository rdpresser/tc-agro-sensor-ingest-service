namespace TC.Agro.SensorIngest.Domain.ValueObjects
{
    public sealed record SensorStatus
    {
        public static readonly ValidationError Required = new("SensorStatus.Required", "Sensor status is required.");
        public static readonly ValidationError InvalidValue = new("SensorStatus.InvalidValue", "Invalid sensor status value.");

        public const string Online = "Online";
        public const string Warning = "Warning";
        public const string Offline = "Offline";

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            Online,
            Warning,
            Offline
        };

        public string Value { get; }

        private SensorStatus(string value)
        {
            Value = value;
        }

        public static Result<SensorStatus> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Invalid(Required);

            var trimmedValue = value.Trim();

            if (!ValidStatuses.Contains(trimmedValue))
                return Result.Invalid(InvalidValue);

            string normalizedValue = ValidStatuses.First(s => s.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase));
            return Result.Success(new SensorStatus(normalizedValue));
        }

        public static Result<SensorStatus> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Invalid(Required);

            if (!ValidStatuses.Contains(value))
                return Result.Invalid(InvalidValue);

            return Result.Success(new SensorStatus(value));
        }

        public static SensorStatus CreateOnline() => new(Online);
        public static SensorStatus CreateWarning() => new(Warning);
        public static SensorStatus CreateOffline() => new(Offline);

        public bool IsOnline => Value == Online;
        public bool IsWarning => Value == Warning;
        public bool IsOffline => Value == Offline;

        public static IReadOnlyCollection<string> GetValidStatuses() => ValidStatuses.ToList().AsReadOnly();

        public static implicit operator string(SensorStatus status) => status.Value;

        public override string ToString() => Value;
    }
}
