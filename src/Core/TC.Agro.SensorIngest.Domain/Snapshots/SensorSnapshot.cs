using System.Text.Json.Serialization;
using TC.Agro.SensorIngest.Domain.Aggregates;

namespace TC.Agro.SensorIngest.Domain.Snapshots
{
    public sealed class SensorSnapshot
    {
        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }
        [JsonIgnore]
        public OwnerSnapshot Owner { get; private set; } = default!;
        public Guid PropertyId { get; private set; }
        public Guid PlotId { get; private set; }

        public string? Label { get; private set; } = default!;
        public string PlotName { get; private set; } = default!;
        public string PropertyName { get; private set; } = default!;
        public double? PlotLatitude { get; private set; }
        public double? PlotLongitude { get; private set; }
        public string? PlotBoundaryGeoJson { get; private set; }
        public string? Status { get; private set; }
        public string? StatusChangeReason { get; private set; } = default!;

        public bool IsActive { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }

        [JsonIgnore]
        public ICollection<SensorReadingAggregate> SensorReadings { get; private set; } = [];

        private SensorSnapshot() { }

        [JsonConstructor]
        public SensorSnapshot(
            Guid id,
            Guid ownerId,
            Guid propertyId,
            Guid plotId,
            string? label,
            string plotName,
            string propertyName,
            double? plotLatitude,
            double? plotLongitude,
            string? plotBoundaryGeoJson,
            bool isActive,
            DateTimeOffset createdAt,
            DateTimeOffset? updatedAt,
            string? status = null)
        {
            Id = id;
            OwnerId = ownerId;
            PropertyId = propertyId;
            PlotId = plotId;
            Label = label;
            PlotName = plotName;
            PropertyName = propertyName;
            PlotLatitude = plotLatitude;
            PlotLongitude = plotLongitude;
            PlotBoundaryGeoJson = plotBoundaryGeoJson;
            IsActive = isActive;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Status = status;
        }

        public static SensorSnapshot Create(
            Guid id,
            Guid ownerId,
            Guid propertyId,
            Guid plotId,
            string? label,
            string plotName,
            string propertyName,
            double? plotLatitude = null,
            double? plotLongitude = null,
            string? plotBoundaryGeoJson = null,
            string? status = null)
        {
            var now = DateTimeOffset.UtcNow;

            return new SensorSnapshot(
                id,
                ownerId,
                propertyId,
                plotId,
                label,
                plotName,
                propertyName,
                plotLatitude,
                plotLongitude,
                plotBoundaryGeoJson,
                true,
                now,
                null,
                status);
        }

        public static SensorSnapshot Create(
            Guid id,
            Guid ownerId,
            Guid propertyId,
            Guid plotId,
            string? label,
            string plotName,
            string propertyName,
            DateTimeOffset createdAt,
            double? plotLatitude = null,
            double? plotLongitude = null,
            string? plotBoundaryGeoJson = null,
            string? status = null)
        {
            return new SensorSnapshot(
                id,
                ownerId,
                propertyId,
                plotId,
                label,
                plotName,
                propertyName,
                plotLatitude,
                plotLongitude,
                plotBoundaryGeoJson,
                true,
                createdAt,
                null,
                status);
        }

        // Reactivation when status returns to Active
        public void Reactivate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
        // Atualização quando vier evento SensorUpdated ou PlotUpdated
        public void Update(
            Guid ownerId,
            Guid propertyId,
            Guid plotId,
            string sensorName,
            string plotName,
            string propertyName,
            string status,
            string? reason,
            double? plotLatitude = null,
            double? plotLongitude = null,
            string? plotBoundaryGeoJson = null)
        {
            OwnerId = ownerId;
            PropertyId = propertyId;
            PlotId = plotId;
            Status = status;
            Label = sensorName;
            PlotName = plotName;
            PropertyName = propertyName;
            PlotLatitude = plotLatitude;
            PlotLongitude = plotLongitude;
            PlotBoundaryGeoJson = plotBoundaryGeoJson;
            UpdatedAt = DateTimeOffset.UtcNow;
            StatusChangeReason = reason;
        }

        public void Delete()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
