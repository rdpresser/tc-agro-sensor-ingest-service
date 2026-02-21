using TC.Agro.SensorIngest.Domain.Aggregates;

namespace TC.Agro.SensorIngest.Tests.Domain.Aggregates
{
    public class SensorReadingAggregateTests
    {
        #region Create - Valid Cases

        [Fact]
        public void Create_WithValidData_ShouldSucceed()
        {
            var sensorId = Guid.NewGuid();
            var time = DateTime.UtcNow;

            var result = SensorReadingAggregate.Create(
                sensorId: sensorId,
                time: time,
                temperature: 25.0,
                humidity: 60.0,
                soilMoisture: 40.0,
                rainfall: null,
                batteryLevel: 85.0);

            result.IsSuccess.ShouldBeTrue();
            result.Value.SensorId.ShouldBe(sensorId);
            result.Value.Temperature.ShouldBe(25.0);
            result.Value.Humidity.ShouldBe(60.0);
            result.Value.SoilMoisture.ShouldBe(40.0);
            result.Value.Rainfall.ShouldBeNull();
            result.Value.BatteryLevel.ShouldBe(85.0);
            result.Value.IsActive.ShouldBeTrue();
            result.Value.UncommittedEvents.Count.ShouldBe(1);
        }

        [Fact]
        public void Create_WithOnlyTemperature_ShouldSucceed()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: 30.0,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Temperature.ShouldBe(30.0);
            result.Value.Humidity.ShouldBeNull();
            result.Value.SoilMoisture.ShouldBeNull();
            result.Value.Rainfall.ShouldBeNull();
        }

        [Fact]
        public void Create_WithOnlyHumidity_ShouldSucceed()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: null,
                humidity: 75.0,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Humidity.ShouldBe(75.0);
        }

        [Fact]
        public void Create_WithOnlySoilMoisture_ShouldSucceed()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: null,
                humidity: null,
                soilMoisture: 55.0,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeTrue();
            result.Value.SoilMoisture.ShouldBe(55.0);
        }

        [Fact]
        public void Create_WithOnlyRainfall_ShouldSucceed()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: null,
                humidity: null,
                soilMoisture: null,
                rainfall: 12.5,
                batteryLevel: null);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Rainfall.ShouldBe(12.5);
        }

        [Fact]
        public void Create_WithBoundaryTemperatureValues_ShouldSucceed()
        {
            var resultMin = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: -50.0,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            var resultMax = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: 70.0,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            resultMin.IsSuccess.ShouldBeTrue();
            resultMax.IsSuccess.ShouldBeTrue();
        }

        #endregion

        #region Create - Invalid Cases

        [Fact]
        public void Create_WithEmptySensorId_ShouldFail()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.Empty,
                time: DateTime.UtcNow,
                temperature: 25.0,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorId.Required");
        }

        [Fact]
        public void Create_WithDefaultTime_ShouldFail()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: default,
                temperature: 25.0,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Time.Required");
        }

        [Fact]
        public void Create_WithFutureTime_ShouldFail()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow.AddMinutes(10),
                temperature: 25.0,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Time.FutureNotAllowed");
        }

        [Fact]
        public void Create_WithNoMetrics_ShouldFail()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: null,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Metrics.Required");
        }

        [Theory]
        [InlineData(-51)]
        [InlineData(71)]
        public void Create_WithTemperatureOutOfRange_ShouldFail(double temperature)
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: temperature,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Temperature.OutOfRange");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Create_WithHumidityOutOfRange_ShouldFail(double humidity)
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: null,
                humidity: humidity,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Humidity.OutOfRange");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Create_WithSoilMoistureOutOfRange_ShouldFail(double soilMoisture)
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: null,
                humidity: null,
                soilMoisture: soilMoisture,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SoilMoisture.OutOfRange");
        }

        [Fact]
        public void Create_WithNegativeRainfall_ShouldFail()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: null,
                humidity: null,
                soilMoisture: null,
                rainfall: -1,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Rainfall.OutOfRange");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Create_WithBatteryLevelOutOfRange_ShouldFail(double batteryLevel)
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow,
                temperature: 25.0,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: batteryLevel);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "BatteryLevel.OutOfRange");
        }

        [Fact]
        public void Create_WithMultipleErrors_ShouldReturnAllErrors()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.Empty,
                time: default,
                temperature: null,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.Count().ShouldBeGreaterThanOrEqualTo(3);
        }

        #endregion

        #region Domain Events

        [Fact]
        public void Create_ShouldAddSensorReadingCreatedDomainEvent()
        {
            var sensorId = Guid.NewGuid();

            var result = SensorReadingAggregate.Create(
                sensorId: sensorId,
                time: DateTime.UtcNow,
                temperature: 25.0,
                humidity: null,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);

            result.IsSuccess.ShouldBeTrue();
            result.Value.UncommittedEvents.Count.ShouldBe(1);
            result.Value.UncommittedEvents[0]
                .ShouldBeOfType<SensorReadingAggregate.SensorReadingCreatedDomainEvent>();
        }

        #endregion
    }
}
