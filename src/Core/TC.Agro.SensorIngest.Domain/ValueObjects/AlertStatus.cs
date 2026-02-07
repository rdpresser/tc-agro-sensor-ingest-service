namespace TC.Agro.SensorIngest.Domain.ValueObjects
{
    public sealed record AlertStatus
    {
        public static readonly ValidationError Required = new("AlertStatus.Required", "Alert status is required.");
        public static readonly ValidationError InvalidValue = new("AlertStatus.InvalidValue", "Invalid alert status value.");

        public const string Pending = "Pending";
        public const string Resolved = "Resolved";

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            Pending,
            Resolved
        };

        public string Value { get; }

        private AlertStatus(string value)
        {
            Value = value;
        }

        public static Result<AlertStatus> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Invalid(Required);

            var trimmedValue = value.Trim();

            if (!ValidStatuses.Contains(trimmedValue))
                return Result.Invalid(InvalidValue);

            string normalizedValue = ValidStatuses.First(s => s.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase));
            return Result.Success(new AlertStatus(normalizedValue));
        }

        public static Result<AlertStatus> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Invalid(Required);

            return Result.Success(new AlertStatus(value));
        }

        public static AlertStatus CreatePending() => new(Pending);
        public static AlertStatus CreateResolved() => new(Resolved);

        public bool IsPending => Value == Pending;
        public bool IsResolved => Value == Resolved;

        public static IReadOnlyCollection<string> GetValidStatuses() => ValidStatuses.ToList().AsReadOnly();

        public static implicit operator string(AlertStatus status) => status.Value;

        public override string ToString() => Value;
    }
}
