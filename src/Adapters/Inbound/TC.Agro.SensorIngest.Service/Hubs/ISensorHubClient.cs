namespace TC.Agro.SensorIngest.Service.Hubs
{
    public interface ISensorHubClient
    {
        Task SensorReading(SensorReadingRequest reading);
        Task NewAlert(AlertRequest alert);
        Task SensorStatusChanged(SensorStatusChangedRequest data);
    }
}
