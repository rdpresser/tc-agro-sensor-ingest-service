namespace TC.Agro.SensorIngest.Application.UseCases.CreateAlert
{
    internal static class CreateAlertMapper
    {
        public static Result<AlertAggregate> ToAggregate(CreateAlertCommand command)
        {
            return AlertAggregate.Create(
                severity: command.Severity,
                title: command.Title,
                message: command.Message,
                plotId: command.PlotId,
                plotName: command.PlotName,
                sensorId: command.SensorId);
        }

        public static CreateAlertResponse FromAggregate(AlertAggregate aggregate)
        {
            return new CreateAlertResponse(
                Id: aggregate.Id,
                Severity: aggregate.Severity.Value,
                Status: aggregate.Status.Value,
                CreatedAt: aggregate.CreatedAt);
        }
    }
}
