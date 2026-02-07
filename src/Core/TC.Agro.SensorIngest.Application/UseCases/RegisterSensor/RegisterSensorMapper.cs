namespace TC.Agro.SensorIngest.Application.UseCases.RegisterSensor
{
    internal static class RegisterSensorMapper
    {
        public static Result<SensorAggregate> ToAggregate(RegisterSensorCommand command)
        {
            return SensorAggregate.Create(
                sensorId: command.SensorId,
                plotId: command.PlotId,
                plotName: command.PlotName,
                battery: command.Battery);
        }

        public static RegisterSensorResponse FromAggregate(SensorAggregate aggregate)
        {
            return new RegisterSensorResponse(
                Id: aggregate.Id,
                SensorId: aggregate.SensorId,
                PlotId: aggregate.PlotId,
                Status: aggregate.Status.Value);
        }
    }
}
