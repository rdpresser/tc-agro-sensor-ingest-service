namespace TC.Agro.SensorIngest.Service.Hubs
{
    public interface ISensorHubClient
    {
        Task SensorReading(SensorReadingHubDto reading);
        Task NewAlert(AlertHubDto alert);
        Task SensorStatusChanged(SensorStatusChangedDto data);
    }
}
