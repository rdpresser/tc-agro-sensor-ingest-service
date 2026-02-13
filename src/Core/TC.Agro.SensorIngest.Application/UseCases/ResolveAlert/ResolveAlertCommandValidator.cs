namespace TC.Agro.SensorIngest.Application.UseCases.ResolveAlert
{
    public sealed class ResolveAlertCommandValidator : Validator<ResolveAlertCommand>
    {
        public ResolveAlertCommandValidator()
        {
            RuleFor(x => x.AlertId)
                .NotEmpty().WithMessage("AlertId is required.");
        }
    }
}
