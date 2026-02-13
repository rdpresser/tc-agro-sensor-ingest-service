using TC.Agro.SensorIngest.Application.UseCases.CreateAlert;

namespace TC.Agro.SensorIngest.Tests.Application.Validators
{
    public class CreateAlertCommandValidatorTests
    {
        private readonly CreateAlertCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldPass()
        {
            var command = new CreateAlertCommand(
                Severity: "Warning",
                Title: "High Temperature",
                Message: "Temperature exceeded 40C threshold",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                SensorId: Guid.NewGuid());

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_WithEmptySeverity_ShouldFail()
        {
            var command = new CreateAlertCommand(
                Severity: "",
                Title: "Test",
                Message: "Test message",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                SensorId: Guid.NewGuid());

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Severity");
        }

        [Fact]
        public void Validate_WithInvalidSeverity_ShouldFail()
        {
            var command = new CreateAlertCommand(
                Severity: "InvalidSeverity",
                Title: "Test",
                Message: "Test message",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                SensorId: Guid.NewGuid());

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Severity");
        }

        [Fact]
        public void Validate_WithEmptyTitle_ShouldFail()
        {
            var command = new CreateAlertCommand(
                Severity: "Warning",
                Title: "",
                Message: "Test message",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                SensorId: Guid.NewGuid());

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Title");
        }

        [Fact]
        public void Validate_WithEmptyMessage_ShouldFail()
        {
            var command = new CreateAlertCommand(
                Severity: "Warning",
                Title: "Test",
                Message: "",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                SensorId: Guid.NewGuid());

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "Message");
        }

        [Fact]
        public void Validate_WithEmptyPlotId_ShouldFail()
        {
            var command = new CreateAlertCommand(
                Severity: "Warning",
                Title: "Test",
                Message: "Test message",
                PlotId: Guid.Empty,
                PlotName: "Plot Alpha",
                SensorId: Guid.NewGuid());

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "PlotId");
        }

        [Fact]
        public void Validate_WithEmptySensorId_ShouldFail()
        {
            var command = new CreateAlertCommand(
                Severity: "Warning",
                Title: "Test",
                Message: "Test message",
                PlotId: Guid.NewGuid(),
                PlotName: "Plot Alpha",
                SensorId: Guid.Empty);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "SensorId");
        }
    }
}
