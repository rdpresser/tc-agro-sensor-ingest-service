namespace TC.Agro.SensorIngest.Domain.Aggregates
{
    public sealed class SensorReadingAggregate : BaseAggregateRoot
    {
        public Guid SensorId { get; private set; }
        public Guid PlotId { get; private set; }
        public DateTime Time { get; private set; }
        public double? Temperature { get; private set; }
        public double? Humidity { get; private set; }
        public double? SoilMoisture { get; private set; }
        public double? Rainfall { get; private set; }
        public double? BatteryLevel { get; private set; }

        private SensorReadingAggregate(Guid id) : base(id) { }

        // For EF Core
        private SensorReadingAggregate() : base(Guid.Empty) { }

        #region Factories

        public static Result<SensorReadingAggregate> Create(
            Guid sensorId,
            Guid plotId,
            DateTime time,
            double? temperature,
            double? humidity,
            double? soilMoisture,
            double? rainfall,
            double? batteryLevel)
        {
            var errors = new List<ValidationError>();
            errors.AddRange(ValidateSensorId(sensorId));
            errors.AddRange(ValidatePlotId(plotId));
            errors.AddRange(ValidateTime(time));
            errors.AddRange(ValidateMetrics(temperature, humidity, soilMoisture, rainfall, batteryLevel));

            if (errors.Count > 0)
                return Result.Invalid(errors.ToArray());

            return CreateAggregate(sensorId, plotId, time, temperature, humidity, soilMoisture, rainfall, batteryLevel);
        }

        private static Result<SensorReadingAggregate> CreateAggregate(
            Guid sensorId,
            Guid plotId,
            DateTime time,
            double? temperature,
            double? humidity,
            double? soilMoisture,
            double? rainfall,
            double? batteryLevel)
        {
            var aggregate = new SensorReadingAggregate(Guid.NewGuid());
            var @event = new SensorReadingCreatedDomainEvent(
                aggregate.Id,
                sensorId,
                plotId,
                time,
                temperature,
                humidity,
                soilMoisture,
                rainfall,
                batteryLevel,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Domain Events Apply

        public void Apply(SensorReadingCreatedDomainEvent @event)
        {
            SetId(@event.AggregateId);
            SensorId = @event.SensorId;
            PlotId = @event.PlotId;
            Time = @event.Time;
            Temperature = @event.Temperature;
            Humidity = @event.Humidity;
            SoilMoisture = @event.SoilMoisture;
            Rainfall = @event.Rainfall;
            BatteryLevel = @event.BatteryLevel;
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case SensorReadingCreatedDomainEvent createdEvent:
                    Apply(createdEvent);
                    break;
            }
        }

        #endregion

        #region Validation Helpers

        private static IEnumerable<ValidationError> ValidateSensorId(Guid sensorId)
        {
            if (sensorId == Guid.Empty)
                yield return new ValidationError($"{nameof(SensorId)}.Required", "SensorId is required.");
        }

        private static IEnumerable<ValidationError> ValidatePlotId(Guid plotId)
        {
            if (plotId == Guid.Empty)
                yield return new ValidationError($"{nameof(PlotId)}.Required", "PlotId is required.");
        }

        private static IEnumerable<ValidationError> ValidateTime(DateTime time)
        {
            if (time == default)
                yield return new ValidationError($"{nameof(Time)}.Required", "Time is required.");
            else if (time > DateTime.UtcNow.AddMinutes(5))
                yield return new ValidationError($"{nameof(Time)}.FutureNotAllowed", "Time cannot be in the future.");
        }

        private static IEnumerable<ValidationError> ValidateMetrics(
            double? temperature,
            double? humidity,
            double? soilMoisture,
            double? rainfall,
            double? batteryLevel)
        {
            if (!temperature.HasValue && !humidity.HasValue && !soilMoisture.HasValue && !rainfall.HasValue)
                yield return new ValidationError("Metrics.Required", "At least one metric (temperature, humidity, soilMoisture, or rainfall) is required.");

            if (temperature.HasValue && (temperature < -50 || temperature > 70))
                yield return new ValidationError($"{nameof(Temperature)}.OutOfRange", "Temperature must be between -50 and 70 degrees Celsius.");

            if (humidity.HasValue && (humidity < 0 || humidity > 100))
                yield return new ValidationError($"{nameof(Humidity)}.OutOfRange", "Humidity must be between 0 and 100 percent.");

            if (soilMoisture.HasValue && (soilMoisture < 0 || soilMoisture > 100))
                yield return new ValidationError($"{nameof(SoilMoisture)}.OutOfRange", "Soil moisture must be between 0 and 100 percent.");

            if (rainfall.HasValue && rainfall < 0)
                yield return new ValidationError($"{nameof(Rainfall)}.OutOfRange", "Rainfall cannot be negative.");

            if (batteryLevel.HasValue && (batteryLevel < 0 || batteryLevel > 100))
                yield return new ValidationError($"{nameof(BatteryLevel)}.OutOfRange", "Battery level must be between 0 and 100 percent.");
        }

        #endregion

        #region Domain Events

        public record SensorReadingCreatedDomainEvent(
            Guid AggregateId,
            Guid SensorId,
            Guid PlotId,
            DateTime Time,
            double? Temperature,
            double? Humidity,
            double? SoilMoisture,
            double? Rainfall,
            double? BatteryLevel,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
