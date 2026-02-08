namespace TC.Agro.SensorIngest.Domain.ValueObjects
{
    public sealed record AlertSeverity
    {
        public static readonly ValidationError Required = new("AlertSeverity.Required", "Alert severity is required.");
        public static readonly ValidationError InvalidValue = new("AlertSeverity.InvalidValue", "Invalid alert severity value.");

        public const string Critical = "Critical";
        public const string Warning = "Warning";
        public const string Info = "Info";

        private static readonly HashSet<string> ValidSeverities = new(StringComparer.OrdinalIgnoreCase)
        {
            Critical,
            Warning,
            Info
        };

        public string Value { get; }

        private AlertSeverity(string value)
        {
            Value = value;
        }

        public static Result<AlertSeverity> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Invalid(Required);

            var trimmedValue = value.Trim();

            if (!ValidSeverities.Contains(trimmedValue))
                return Result.Invalid(InvalidValue);

            string normalizedValue = ValidSeverities.First(s => s.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase));
            return Result.Success(new AlertSeverity(normalizedValue));
        }

        public static Result<AlertSeverity> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Invalid(Required);

            return Result.Success(new AlertSeverity(value));
        }

        public static IReadOnlyCollection<string> GetValidSeverities() => ValidSeverities.ToList().AsReadOnly();

        public static implicit operator string(AlertSeverity severity) => severity.Value;

        public override string ToString() => Value;
    }
}
