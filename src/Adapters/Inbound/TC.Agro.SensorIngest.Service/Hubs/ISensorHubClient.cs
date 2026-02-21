namespace TC.Agro.SensorIngest.Service.Hubs
{
    public interface ISensorHubClient
    {
        Task SensorReading(SensorReadingRequest reading);
        Task SensorStatusChanged(SensorStatusChangedRequest data);
    }
}
