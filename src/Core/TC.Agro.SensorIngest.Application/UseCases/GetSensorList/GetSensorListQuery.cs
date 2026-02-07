namespace TC.Agro.SensorIngest.Application.UseCases.GetSensorList
{
    public sealed record GetSensorListQuery(
        Guid? PlotId = null) : IBaseQuery<GetSensorListResponse>;
}
