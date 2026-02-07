namespace TC.Agro.SensorIngest.Application.UseCases.GetSensorList
{
    public sealed record GetSensorListResponse(IReadOnlyList<SensorListDto> Sensors);
}
