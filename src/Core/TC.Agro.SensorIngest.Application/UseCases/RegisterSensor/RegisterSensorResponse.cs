namespace TC.Agro.SensorIngest.Application.UseCases.RegisterSensor
{
    public sealed record RegisterSensorResponse(
        Guid Id,
        Guid SensorId,
        Guid PlotId,
        string Status,
        string Message = "Sensor registered successfully");
}
