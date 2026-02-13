namespace TC.Agro.SensorIngest.Application.UseCases.CreateReading
{
    public sealed class CreateReadingCommandValidator : Validator<CreateReadingCommand>
    {
        public CreateReadingCommandValidator()
        {
            RuleFor(x => x.SensorId)
                .NotEmpty().WithMessage("SensorId is required.");

            RuleFor(x => x.PlotId)
                .NotEmpty().WithMessage("PlotId is required.");

            RuleFor(x => x.Timestamp)
                .NotEmpty().WithMessage("Timestamp is required.")
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5)).WithMessage("Timestamp cannot be in the future.");

            RuleFor(x => x.Temperature)
                .InclusiveBetween(-50, 70).When(x => x.Temperature.HasValue)
                .WithMessage("Temperature must be between -50 and 70 degrees Celsius.");

            RuleFor(x => x.Humidity)
                .InclusiveBetween(0, 100).When(x => x.Humidity.HasValue)
                .WithMessage("Humidity must be between 0 and 100 percent.");

            RuleFor(x => x.SoilMoisture)
                .InclusiveBetween(0, 100).When(x => x.SoilMoisture.HasValue)
                .WithMessage("Soil moisture must be between 0 and 100 percent.");

            RuleFor(x => x.Rainfall)
                .GreaterThanOrEqualTo(0).When(x => x.Rainfall.HasValue)
                .WithMessage("Rainfall cannot be negative.");

            RuleFor(x => x.BatteryLevel)
                .InclusiveBetween(0, 100).When(x => x.BatteryLevel.HasValue)
                .WithMessage("Battery level must be between 0 and 100 percent.");

            RuleFor(x => x)
                .Must(x => x.Temperature.HasValue || x.Humidity.HasValue || x.SoilMoisture.HasValue || x.Rainfall.HasValue)
                .WithMessage("At least one metric (temperature, humidity, soilMoisture, or rainfall) is required.");
        }
    }
}
