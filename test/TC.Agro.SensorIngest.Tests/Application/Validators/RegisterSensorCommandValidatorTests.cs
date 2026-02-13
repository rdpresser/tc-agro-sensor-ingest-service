using TC.Agro.SensorIngest.Application.UseCases.RegisterSensor;

namespace TC.Agro.SensorIngest.Tests.Application.Validators
{
    public class RegisterSensorCommandValidatorTests
    {
        private readonly RegisterSensorCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldPass()
        {
            var command = new RegisterSensorCommand(
                SensorId: "SENSOR-001",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                Battery: 95.0);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_WithEmptySensorId_ShouldFail()
        {
            var command = new RegisterSensorCommand(
                SensorId: "",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                Battery: 95.0);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "SensorId");
        }

        [Fact]
        public void Validate_WithEmptyPlotId_ShouldFail()
        {
            var command = new RegisterSensorCommand(
                SensorId: "SENSOR-001",
                PlotId: Guid.Empty,
                PlotName: "Plot Alpha",
                Battery: 95.0);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "PlotId");
        }

        [Fact]
        public void Validate_WithEmptyPlotName_ShouldFail()
        {
            var command = new RegisterSensorCommand(
                SensorId: "SENSOR-001",
                PlotId: Guid.NewGuid(),
                PlotName: "",
                Battery: 95.0);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "PlotName");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Validate_WithInvalidBattery_ShouldFail(double battery)
        {
            var command = new RegisterSensorCommand(
                SensorId: "SENSOR-001",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                Battery: battery);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Battery");
        }
    }
}
