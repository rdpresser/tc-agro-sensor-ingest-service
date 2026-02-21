using TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings;

namespace TC.Agro.SensorIngest.Tests.Application.Validators
{
    public class CreateBatchReadingsCommandValidatorTests
    {
        private readonly CreateBatchReadingsCommandValidator _validator = new();

        #region Valid Cases

        [Fact]
        public void Validate_WithValidSingleReading_ShouldPass()
        {
            var command = new CreateBatchReadingsCommand(
                Readings:
                [
                    new SensorReadingInput(
                        SensorId: Guid.NewGuid(),
                        Timestamp: DateTime.UtcNow,
                        Temperature: 25.0,
                        Humidity: null,
                        SoilMoisture: null,
                        Rainfall: null,
                        BatteryLevel: 80.0)
                ]);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_WithMultipleValidReadings_ShouldPass()
        {
            var command = new CreateBatchReadingsCommand(
                Readings:
                [
                    new SensorReadingInput(
                        SensorId: Guid.NewGuid(),
                        Timestamp: DateTime.UtcNow,
                        Temperature: 25.0,
                        Humidity: null,
                        SoilMoisture: null,
                        Rainfall: null,
                        BatteryLevel: null),
                    new SensorReadingInput(
                        SensorId: Guid.NewGuid(),
                        Timestamp: DateTime.UtcNow,
                        Temperature: null,
                        Humidity: 70.0,
                        SoilMoisture: null,
                        Rainfall: null,
                        BatteryLevel: null)
                ]);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        #endregion

        #region Empty Readings

        [Fact]
        public void Validate_WithEmptyReadings_ShouldFail()
        {
            var command = new CreateBatchReadingsCommand(
                Readings: []);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Readings");
        }

        #endregion

        #region Child Validation - SensorId

        [Fact]
        public void Validate_WithEmptySensorIdInReading_ShouldFail()
        {
            var command = new CreateBatchReadingsCommand(
                Readings:
                [
                    new SensorReadingInput(
                        SensorId: Guid.Empty,
                        Timestamp: DateTime.UtcNow,
                        Temperature: 25.0,
                        Humidity: null,
                        SoilMoisture: null,
                        Rainfall: null,
                        BatteryLevel: null)
                ]);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
        }

        #endregion

        #region Child Validation - Timestamp

        [Fact]
        public void Validate_WithEmptyTimestampInReading_ShouldFail()
        {
            var command = new CreateBatchReadingsCommand(
                Readings:
                [
                    new SensorReadingInput(
                        SensorId: Guid.NewGuid(),
                        Timestamp: default,
                        Temperature: 25.0,
                        Humidity: null,
                        SoilMoisture: null,
                        Rainfall: null,
                        BatteryLevel: null)
                ]);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
        }

        #endregion

        #region Child Validation - At Least One Metric

        [Fact]
        public void Validate_WithNoMetricsInReading_ShouldFail()
        {
            var command = new CreateBatchReadingsCommand(
                Readings:
                [
                    new SensorReadingInput(
                        SensorId: Guid.NewGuid(),
                        Timestamp: DateTime.UtcNow,
                        Temperature: null,
                        Humidity: null,
                        SoilMoisture: null,
                        Rainfall: null,
                        BatteryLevel: null)
                ]);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
        }

        #endregion
    }
}
