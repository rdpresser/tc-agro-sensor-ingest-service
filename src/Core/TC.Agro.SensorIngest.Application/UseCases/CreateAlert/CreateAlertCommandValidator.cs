using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Application.UseCases.CreateAlert
{
    public sealed class CreateAlertCommandValidator : Validator<CreateAlertCommand>
    {
        public CreateAlertCommandValidator()
        {
            RuleFor(x => x.Severity)
                .NotEmpty().WithMessage("Severity is required.")
                .Must(s => AlertSeverity.Create(s).IsSuccess)
                .WithMessage("Invalid severity value. Valid values: Critical, Warning, Info.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must be at most 200 characters.");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required.")
                .MaximumLength(1000).WithMessage("Message must be at most 1000 characters.");

            RuleFor(x => x.PlotId)
                .NotEmpty().WithMessage("PlotId is required.");

            RuleFor(x => x.PlotName)
                .NotEmpty().WithMessage("PlotName is required.")
                .MaximumLength(200).WithMessage("PlotName must be at most 200 characters.");

            RuleFor(x => x.SensorId)
                .NotEmpty().WithMessage("SensorId is required.")
                .MaximumLength(100).WithMessage("SensorId must be at most 100 characters.");
        }
    }
}
