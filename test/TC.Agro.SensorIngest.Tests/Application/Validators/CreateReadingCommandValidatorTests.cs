using TC.Agro.SensorIngest.Application.UseCases.CreateReading;

namespace TC.Agro.SensorIngest.Tests.Application.Validators
{
    public class CreateReadingCommandValidatorTests
    {
        private readonly CreateReadingCommandValidator _validator = new();

        #region Valid Cases

        [Fact]
        public void Validate_WithValidCommand_ShouldPass()
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: 25.0,
                Humidity: 60.0,
                SoilMoisture: 40.0,
                Rainfall: null,
                BatteryLevel: 85.0);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_WithOnlyTemperature_ShouldPass()
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: 25.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        #endregion

        #region SensorId Validation

        [Fact]
        public void Validate_WithEmptySensorId_ShouldFail()
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.Empty,
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: 25.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "SensorId");
        }

        #endregion

        #region PlotId Validation

        [Fact]
        public void Validate_WithEmptyPlotId_ShouldFail()
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.Empty,
                Timestamp: DateTime.UtcNow,
                Temperature: 25.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "PlotId");
        }

        #endregion

        #region Timestamp Validation

        [Fact]
        public void Validate_WithEmptyTimestamp_ShouldFail()
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: default,
                Temperature: 25.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Timestamp");
        }

        #endregion

        #region Metric Range Validation

        [Theory]
        [InlineData(-51)]
        [InlineData(71)]
        public void Validate_WithTemperatureOutOfRange_ShouldFail(double temperature)
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: temperature,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Temperature");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Validate_WithHumidityOutOfRange_ShouldFail(double humidity)
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: null,
                Humidity: humidity,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Humidity");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Validate_WithSoilMoistureOutOfRange_ShouldFail(double soilMoisture)
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: null,
                Humidity: null,
                SoilMoisture: soilMoisture,
                Rainfall: null,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "SoilMoisture");
        }

        [Fact]
        public void Validate_WithNegativeRainfall_ShouldFail()
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: null,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: -1,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Rainfall");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Validate_WithBatteryLevelOutOfRange_ShouldFail(double batteryLevel)
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: 25.0,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: batteryLevel);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "BatteryLevel");
        }

        #endregion

        #region At Least One Metric Required

        [Fact]
        public void Validate_WithNoMetrics_ShouldFail()
        {
            var command = new CreateReadingCommand(
                SensorId: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Temperature: null,
                Humidity: null,
                SoilMoisture: null,
                Rainfall: null,
                BatteryLevel: null);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
        }

        #endregion
    }
}
