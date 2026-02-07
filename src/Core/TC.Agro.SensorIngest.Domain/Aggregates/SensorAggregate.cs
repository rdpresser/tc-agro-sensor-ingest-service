using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Domain.Aggregates
{
    public sealed class SensorAggregate : BaseAggregateRoot
    {
        public string SensorId { get; private set; } = default!;
        public Guid PlotId { get; private set; }
        public string PlotName { get; private set; } = default!;
        public SensorStatus Status { get; private set; } = default!;
        public double Battery { get; private set; }
        public DateTimeOffset? LastReadingAt { get; private set; }
        public double? LastTemperature { get; private set; }
        public double? LastHumidity { get; private set; }
        public double? LastSoilMoisture { get; private set; }

        private SensorAggregate(Guid id) : base(id) { }

        // For EF Core
        private SensorAggregate() : base(Guid.Empty) { }

        #region Factories

        public static Result<SensorAggregate> Create(
            string sensorId,
            Guid plotId,
            string plotName,
            double battery)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(sensorId))
                errors.Add(new ValidationError("SensorId.Required", "SensorId is required."));
            else if (sensorId.Length > 100)
                errors.Add(new ValidationError("SensorId.TooLong", "SensorId must be at most 100 characters."));

            if (plotId == Guid.Empty)
                errors.Add(new ValidationError("PlotId.Required", "PlotId is required."));

            if (string.IsNullOrWhiteSpace(plotName))
                errors.Add(new ValidationError("PlotName.Required", "PlotName is required."));

            if (battery < 0 || battery > 100)
                errors.Add(new ValidationError("Battery.OutOfRange", "Battery level must be between 0 and 100."));

            if (errors.Count > 0)
                return Result.Invalid(errors.ToArray());

            var aggregate = new SensorAggregate(Guid.NewGuid());
            var @event = new SensorRegisteredDomainEvent(
                aggregate.Id,
                sensorId,
                plotId,
                plotName,
                battery,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Methods

        public void UpdateLastReading(
            double? temperature,
            double? humidity,
            double? soilMoisture,
            double battery,
            DateTimeOffset readingTime)
        {
            LastTemperature = temperature;
            LastHumidity = humidity;
            LastSoilMoisture = soilMoisture;
            Battery = battery;
            LastReadingAt = readingTime;
            SetUpdatedAt(DateTimeOffset.UtcNow);
        }

        public void UpdateStatus(string status)
        {
            var statusResult = SensorStatus.Create(status);
            if (!statusResult.IsSuccess)
                return;

            var oldStatus = Status.Value;
            Status = statusResult.Value;
            SetUpdatedAt(DateTimeOffset.UtcNow);

            if (oldStatus != status)
            {
                AddNewEvent(new SensorStatusChangedDomainEvent(
                    Id,
                    SensorId,
                    oldStatus,
                    status,
                    DateTimeOffset.UtcNow));
            }
        }

        public void Deactivate()
        {
            SetDeactivate();
            Status = SensorStatus.CreateOffline();
            SetUpdatedAt(DateTimeOffset.UtcNow);
        }

        #endregion

        #region Domain Events Apply

        public void Apply(SensorRegisteredDomainEvent @event)
        {
            SetId(@event.AggregateId);
            SensorId = @event.SensorId;
            PlotId = @event.PlotId;
            PlotName = @event.PlotName;
            Battery = @event.Battery;
            Status = SensorStatus.CreateOnline();
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case SensorRegisteredDomainEvent registeredEvent:
                    Apply(registeredEvent);
                    break;
            }
        }

        #endregion

        #region Domain Events

        public record SensorRegisteredDomainEvent(
            Guid AggregateId,
            string SensorId,
            Guid PlotId,
            string PlotName,
            double Battery,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record SensorStatusChangedDomainEvent(
            Guid AggregateId,
            string SensorId,
            string OldStatus,
            string NewStatus,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
