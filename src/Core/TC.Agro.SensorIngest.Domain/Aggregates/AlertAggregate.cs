using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Domain.Aggregates
{
    public sealed class AlertAggregate : BaseAggregateRoot
    {
        public AlertSeverity Severity { get; private set; } = default!;
        public string Title { get; private set; } = default!;
        public string Message { get; private set; } = default!;
        public Guid PlotId { get; private set; }
        public string PlotName { get; private set; } = default!;
        public string SensorId { get; private set; } = default!;
        public AlertStatus Status { get; private set; } = default!;
        public DateTimeOffset? ResolvedAt { get; private set; }

        private AlertAggregate(Guid id) : base(id) { }

        // For EF Core
        private AlertAggregate() : base(Guid.Empty) { }

        #region Factories

        public static Result<AlertAggregate> Create(
            string severity,
            string title,
            string message,
            Guid plotId,
            string plotName,
            string sensorId)
        {
            var errors = new List<ValidationError>();

            var severityResult = AlertSeverity.Create(severity);
            if (!severityResult.IsSuccess)
                errors.AddRange(severityResult.ValidationErrors);

            if (string.IsNullOrWhiteSpace(title))
                errors.Add(new ValidationError("Title.Required", "Title is required."));
            else if (title.Length > 200)
                errors.Add(new ValidationError("Title.TooLong", "Title must be at most 200 characters."));

            if (string.IsNullOrWhiteSpace(message))
                errors.Add(new ValidationError("Message.Required", "Message is required."));
            else if (message.Length > 1000)
                errors.Add(new ValidationError("Message.TooLong", "Message must be at most 1000 characters."));

            if (plotId == Guid.Empty)
                errors.Add(new ValidationError("PlotId.Required", "PlotId is required."));

            if (string.IsNullOrWhiteSpace(plotName))
                errors.Add(new ValidationError("PlotName.Required", "PlotName is required."));
            else if (plotName.Length > 200)
                errors.Add(new ValidationError("PlotName.TooLong", "PlotName must be at most 200 characters."));

            if (string.IsNullOrWhiteSpace(sensorId))
                errors.Add(new ValidationError("SensorId.Required", "SensorId is required."));
            else if (sensorId.Length > 100)
                errors.Add(new ValidationError("SensorId.TooLong", "SensorId must be at most 100 characters."));

            if (errors.Count > 0)
                return Result.Invalid(errors.ToArray());

            var aggregate = new AlertAggregate(Guid.NewGuid());
            var @event = new AlertCreatedDomainEvent(
                aggregate.Id,
                severityResult.Value.Value,
                title,
                message,
                plotId,
                plotName,
                sensorId,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Methods

        public Result Resolve()
        {
            if (Status.IsResolved)
                return Result.Error("Alert is already resolved.");

            Status = AlertStatus.CreateResolved();
            ResolvedAt = DateTimeOffset.UtcNow;
            SetUpdatedAt(DateTimeOffset.UtcNow);

            AddNewEvent(new AlertResolvedDomainEvent(
                Id,
                SensorId,
                DateTimeOffset.UtcNow));

            return Result.Success();
        }

        #endregion

        #region Domain Events Apply

        public void Apply(AlertCreatedDomainEvent @event)
        {
            SetId(@event.AggregateId);
            Severity = AlertSeverity.FromDb(@event.Severity).Value;
            Title = @event.Title;
            Message = @event.Message;
            PlotId = @event.PlotId;
            PlotName = @event.PlotName;
            SensorId = @event.SensorId;
            Status = AlertStatus.CreatePending();
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case AlertCreatedDomainEvent createdEvent:
                    Apply(createdEvent);
                    break;
            }
        }

        #endregion

        #region Domain Events

        public record AlertCreatedDomainEvent(
            Guid AggregateId,
            string Severity,
            string Title,
            string Message,
            Guid PlotId,
            string PlotName,
            string SensorId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record AlertResolvedDomainEvent(
            Guid AggregateId,
            string SensorId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
