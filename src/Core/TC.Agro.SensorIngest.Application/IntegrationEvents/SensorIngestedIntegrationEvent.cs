namespace TC.Agro.SensorIngest.Application.IntegrationEvents
{
    /// <summary>
    /// Integration event published when a sensor reading is successfully ingested.
    /// Consumed by Analytics.Worker for rule evaluation and alert generation.
    /// </summary>
    public sealed record SensorIngestedIntegrationEvent(
        Guid EventId,
        Guid AggregateId,
        DateTimeOffset OccurredOn,
        string EventName,
        IDictionary<string, Guid>? RelatedIds,
        string SensorId,
        Guid PlotId,
        DateTime Time,
        double? Temperature,
        double? Humidity,
        double? SoilMoisture,
        double? Rainfall,
        double? BatteryLevel
    ) : BaseIntegrationEvent(EventId, AggregateId, OccurredOn, EventName, RelatedIds);
}
