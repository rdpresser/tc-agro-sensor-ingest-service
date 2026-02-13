namespace TC.Agro.SensorIngest.Service.Hubs
{
    public sealed record SensorStatusChangedRequest(
        string SensorId,
        string Status);
}
