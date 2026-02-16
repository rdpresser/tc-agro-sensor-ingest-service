using TC.Agro.SensorIngest.Domain.Aggregates;

namespace TC.Agro.SensorIngest.Domain.Snapshots
{
    public sealed class SensorSnapshot
    {
        public Guid Id { get; private set; } // SensorId
        public Guid OwnerId { get; private set; }
        public OwnerSnapshot Owner { get; private set; } = default!;
        public Guid PropertyId { get; private set; }
        public Guid PlotId { get; private set; }

        public string SensorName { get; private set; } = default!;
        public string PlotName { get; private set; } = default!;
        public string PropertyName { get; private set; } = default!;

        public bool IsActive { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }

        public ICollection<SensorReadingAggregate> SensorReadings { get; private set; } = [];

        private SensorSnapshot() { } // EF

        private SensorSnapshot(
            Guid id,
            Guid ownerId,
            Guid propertyId,
            Guid plotId,
            string sensorName,
            string plotName,
            string propertyName,
            bool isActive,
            DateTimeOffset createdAt,
            DateTimeOffset? updatedAt)
        {
            Id = id;
            OwnerId = ownerId;
            PropertyId = propertyId;
            PlotId = plotId;
            SensorName = sensorName;
            PlotName = plotName;
            PropertyName = propertyName;
            IsActive = isActive;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        // Factory usada quando chega evento SensorRegistered
        public static SensorSnapshot Create(
            Guid id,
            Guid ownerId,
            Guid propertyId,
            Guid plotId,
            string sensorName,
            string plotName,
            string propertyName)
        {
            var now = DateTimeOffset.UtcNow;

            return new SensorSnapshot(
                id,
                ownerId,
                propertyId,
                plotId,
                sensorName,
                plotName,
                propertyName,
                true,
                now,
                null);
        }

        // Factory quando evento já traz createdAt
        public static SensorSnapshot Create(
            Guid id,
            Guid ownerId,
            Guid propertyId,
            Guid plotId,
            string sensorName,
            string plotName,
            string propertyName,
            DateTimeOffset createdAt)
        {
            return new SensorSnapshot(
                id,
                ownerId,
                propertyId,
                plotId,
                sensorName,
                plotName,
                propertyName,
                true,
                createdAt,
                null);
        }

        ////// Atualização quando vier evento SensorUpdated ou PlotUpdated
        ////public void Update(
        ////    Guid ownerId,
        ////    Guid propertyId,
        ////    Guid plotId,
        ////    string sensorName,
        ////    string plotName,
        ////    string propertyName,
        ////    bool isActive)
        ////{
        ////    OwnerId = ownerId;
        ////    PropertyId = propertyId;
        ////    PlotId = plotId;
        ////    SensorName = sensorName;
        ////    PlotName = plotName;
        ////    PropertyName = propertyName;
        ////    IsActive = isActive;
        ////    UpdatedAt = DateTimeOffset.UtcNow;
        ////}

        // Desativação quando sensor for removido
        public void Delete()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
