namespace TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings
{
    public sealed class CreateBatchReadingsCommandValidator : Validator<CreateBatchReadingsCommand>
    {
        public CreateBatchReadingsCommandValidator()
        {
            RuleFor(x => x.Readings)
                .NotEmpty().WithMessage("At least one reading is required.")
                .Must(r => r.Count <= AppConstants.MaxBatchSize)
                .WithMessage($"Batch size cannot exceed {AppConstants.MaxBatchSize} readings.");

            RuleForEach(x => x.Readings).ChildRules(reading =>
            {
                reading.RuleFor(r => r.SensorId)
                    .NotEmpty()
                        .WithMessage("SensorId is required.");

                reading.RuleFor(r => r.PlotId)
                    .NotEmpty()
                        .WithMessage("PlotId is required.");

                reading.RuleFor(r => r.Timestamp)
                    .NotEmpty()
                        .WithMessage("Timestamp is required.");

                reading.RuleFor(r => r)
                    .Must(r => r.Temperature.HasValue || r.Humidity.HasValue || r.SoilMoisture.HasValue || r.Rainfall.HasValue)
                    .WithMessage("At least one metric is required.");
            });
        }
    }
}
