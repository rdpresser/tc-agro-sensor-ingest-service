using TC.Agro.SensorIngest.Application.UseCases.CreateReading;
using TC.Agro.SensorIngest.Domain.Aggregates;

namespace TC.Agro.SensorIngest.Tests.Application.Mappers
{
    public class CreateReadingMapperTests
    {
        #region ToAggregate

        [Fact]
        public void ToAggregate_WithValidCommand_ShouldMapAllFields()
        {
            var sensorId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow.AddMinutes(-5);

            var command = new CreateReadingCommand(
                SensorId: sensorId,
                Timestamp: timestamp,
                Temperature: 25.5,
                Humidity: 60.0,
                SoilMoisture: 40.0,
                Rainfall: 2.5,
                BatteryLevel: 85.0);

            var result = CreateReadingMapper.ToAggregate(command);

            result.IsSuccess.ShouldBeTrue();
            var aggregate = result.Value;
            aggregate.SensorId.ShouldBe(sensorId);
            aggregate.Temperature.ShouldBe(25.5);
            aggregate.Humidity.ShouldBe(60.0);
            aggregate.SoilMoisture.ShouldBe(40.0);
            aggregate.Rainfall.ShouldBe(2.5);
            aggregate.BatteryLevel.ShouldBe(85.0);
        }

        [Fact]
        public void ToAggregate_WithNullOptionalFields_ShouldSucceed()
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow.AddMinutes(-1),
                Temperature: 25.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = CreateReadingMapper.ToAggregate(command);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Humidity.ShouldBeNull();
            result.Value.SoilMoisture.ShouldBeNull();
            result.Value.Rainfall.ShouldBeNull();
            result.Value.BatteryLevel.ShouldBeNull();
        }

        [Fact]
        public void ToAggregate_ShouldPreserveTimestamp()
        {
            var timestamp = DateTime.UtcNow.AddMinutes(-10);

            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                Timestamp: timestamp,
                Temperature: 20.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = CreateReadingMapper.ToAggregate(command);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Time.DateTime.ShouldBe(timestamp, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region FromAggregate

        [Fact]
        public void FromAggregate_WithValidAggregate_ShouldMapToResponse()
        {
            var sensorId = Guid.NewGuid();
            var aggregateResult = SensorReadingAggregate.Create(
                sensorId: sensorId,
                time: DateTime.UtcNow.AddMinutes(-1),
                temperature: 25.0,
                humidity: 60.0,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            aggregateResult.IsSuccess.ShouldBeTrue();

            var response = CreateReadingMapper.FromAggregate(aggregateResult.Value);

            response.SensorReadingId.ShouldBe(aggregateResult.Value.Id);
            response.SensorId.ShouldBe(sensorId);
        }

        #endregion

        #region ToIntegrationEvent

        [Fact]
        public void ToIntegrationEvent_ShouldMapAllDomainEventFields()
        {
            var aggregateId = Guid.NewGuid();
            var sensorId = Guid.NewGuid();
            var time = DateTime.UtcNow.AddMinutes(-5);
            var occurredOn = DateTimeOffset.UtcNow;

            var domainEvent = new SensorReadingAggregate.SensorReadingCreatedDomainEvent(
                SensorReadingId: aggregateId,
                SensorId: sensorId,
                Time: time,
                Temperature: 30.0,
                Humidity: 70.0,
                SoilMoisture: 50.0,
                Rainfall: 1.5,
                BatteryLevel: 90.0,
                OccurredOn: occurredOn);

            var integrationEvent = CreateReadingMapper.ToIntegrationEvent(domainEvent);

            integrationEvent.SensorReadingId.ShouldBe(aggregateId);
            integrationEvent.SensorId.ShouldBe(sensorId);
            integrationEvent.Temperature.ShouldBe(30.0);
            integrationEvent.Humidity.ShouldBe(70.0);
            integrationEvent.SoilMoisture.ShouldBe(50.0);
            integrationEvent.Rainfall.ShouldBe(1.5);
            integrationEvent.BatteryLevel.ShouldBe(90.0);
            integrationEvent.OccurredOn.ShouldBe(occurredOn);
        }

        [Fact]
        public void ToIntegrationEvent_WithNullMetrics_ShouldPreserveNulls()
        {
            var domainEvent = new SensorReadingAggregate.SensorReadingCreatedDomainEvent(
                SensorReadingId: Guid.NewGuid(),
                SensorId: Guid.NewGuid(),
                Time: DateTime.UtcNow.AddMinutes(-1),
                Temperature: 25.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null,
                OccurredOn: DateTimeOffset.UtcNow);

            var integrationEvent = CreateReadingMapper.ToIntegrationEvent(domainEvent);

            integrationEvent.Temperature.ShouldBe(25.0);
            integrationEvent.Humidity.ShouldBeNull();
            integrationEvent.SoilMoisture.ShouldBeNull();
            integrationEvent.Rainfall.ShouldBeNull();
            integrationEvent.BatteryLevel.ShouldBeNull();
        }

        #endregion
    }
}
