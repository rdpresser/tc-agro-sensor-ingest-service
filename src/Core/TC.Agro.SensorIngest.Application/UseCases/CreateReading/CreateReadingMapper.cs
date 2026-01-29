namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    internal static class CreateReadingMapper
    {
        public static Result<SensorReadingAggregate> ToAggregate(CreateReadingCommand command)
        {
            return SensorReadingAggregate.Create(
                sensorId: command.SensorId,
                plotId: command.PlotId,
                time: command.Timestamp,
                temperature: command.Temperature,
                humidity: command.Humidity,
                soilMoisture: command.SoilMoisture,
                rainfall: command.Rainfall,
                batteryLevel: command.BatteryLevel);
        }

        public static CreateReadingResponse FromAggregate(SensorReadingAggregate aggregate)
        {
            return new CreateReadingResponse(
                ReadingId: aggregate.Id,
                SensorId: aggregate.SensorId,
                PlotId: aggregate.PlotId,
                Timestamp: aggregate.Time);
        }

        public static SensorIngestedIntegrationEvent ToIntegrationEvent(
            SensorReadingAggregate.SensorReadingCreatedDomainEvent domainEvent)
        {
            return new SensorIngestedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: domainEvent.AggregateId,
                OccurredOn: domainEvent.OccurredOn,
                EventName: nameof(SensorIngestedIntegrationEvent),
                RelatedIds: new Dictionary<string, Guid> { { "PlotId", domainEvent.PlotId } },
                SensorId: domainEvent.SensorId,
                PlotId: domainEvent.PlotId,
                Time: domainEvent.Time,
                Temperature: domainEvent.Temperature,
                Humidity: domainEvent.Humidity,
                SoilMoisture: domainEvent.SoilMoisture,
                Rainfall: domainEvent.Rainfall,
                BatteryLevel: domainEvent.BatteryLevel);
        }
    }
}
