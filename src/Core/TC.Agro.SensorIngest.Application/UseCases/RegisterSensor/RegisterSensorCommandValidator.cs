namespace TC.Agro.SensorIngest.Application.UseCases.RegisterSensor
{
    public sealed class RegisterSensorCommandValidator : Validator<RegisterSensorCommand>
    {
        public RegisterSensorCommandValidator()
        {
            RuleFor(x => x.SensorId)
                .NotEmpty().WithMessage("SensorId is required.");

            RuleFor(x => x.PlotId)
                .NotEmpty().WithMessage("PlotId is required.");

            RuleFor(x => x.PlotName)
                .NotEmpty().WithMessage("PlotName is required.")
                .MaximumLength(200).WithMessage("PlotName must be at most 200 characters.");

            RuleFor(x => x.Battery)
                .InclusiveBetween(0, 100).WithMessage("Battery level must be between 0 and 100.");
        }
    }
}
