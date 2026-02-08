namespace TC.Agro.SensorIngest.Service.Hubs
{
    public sealed record SensorStatusChangedDto(
        string SensorId,
        string Status);
}
