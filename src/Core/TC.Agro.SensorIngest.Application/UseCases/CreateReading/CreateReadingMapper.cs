using TC.Agro.Contracts.Events.SensorIngested;

namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    internal static class CreateReadingMapper
    {
        public static Result<SensorReadingAggregate> ToAggregate(CreateReadingCommand command)
        {
            return SensorReadingAggregate.Create(
                sensorId: command.SensorId,
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
                SensorReadingId: aggregate.Id,
                SensorId: aggregate.SensorId,
                Timestamp: aggregate.Time.Date);
        }

        public static SensorIngestedIntegrationEvent ToIntegrationEvent(
            SensorReadingAggregate.SensorReadingCreatedDomainEvent domainEvent)
        {
            return new SensorIngestedIntegrationEvent(
                SensorReadingId: domainEvent.AggregateId,
                SensorId: domainEvent.SensorId,
                Time: domainEvent.Time,
                Temperature: domainEvent.Temperature,
                Humidity: domainEvent.Humidity,
                SoilMoisture: domainEvent.SoilMoisture,
                Rainfall: domainEvent.Rainfall,
                BatteryLevel: domainEvent.BatteryLevel,
                OccurredOn: domainEvent.OccurredOn);
        }
    }
}
