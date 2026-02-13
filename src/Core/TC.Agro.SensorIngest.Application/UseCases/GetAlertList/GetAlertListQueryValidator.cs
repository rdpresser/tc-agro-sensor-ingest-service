using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Application.UseCases.GetAlertList
{
    public sealed class GetAlertListQueryValidator : Validator<GetAlertListQuery>
    {
        public GetAlertListQueryValidator()
        {
            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrWhiteSpace(s) || AlertStatus.Create(s).IsSuccess)
                .WithMessage("Invalid status value. Valid values: Pending, Resolved.");
        }
    }
}
