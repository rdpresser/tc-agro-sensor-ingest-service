using TC.Agro.SensorIngest.Application.UseCases.ResolveAlert;

namespace TC.Agro.SensorIngest.Tests.Application.Validators
{
    public class ResolveAlertCommandValidatorTests
    {
        private readonly ResolveAlertCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldPass()
        {
            var command = new ResolveAlertCommand(AlertId: Guid.NewGuid());

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_WithEmptyAlertId_ShouldFail()
        {
            var command = new ResolveAlertCommand(AlertId: Guid.Empty);

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.PropertyName == "AlertId");
        }
    }
}
