using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Tests.Domain.Snapshots
{
    public class SensorSnapshotTests
    {
        #region Create - Without CreatedAt

        [Fact]
        public void Create_WithValidData_ShouldSucceed()
        {
            var sensorId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var propertyId = Guid.NewGuid();
            var plotId = Guid.NewGuid();

            var snapshot = SensorSnapshot.Create(
                id: sensorId,
                ownerId: ownerId,
                propertyId: propertyId,
                plotId: plotId,
                label: "Sensor North 1",
                plotName: "Plot Alpha",
                propertyName: "South Farm");

            snapshot.Id.ShouldBe(sensorId);
            snapshot.OwnerId.ShouldBe(ownerId);
            snapshot.PropertyId.ShouldBe(propertyId);
            snapshot.PlotId.ShouldBe(plotId);
            snapshot.Label.ShouldBe("Sensor North 1");
            snapshot.PlotName.ShouldBe("Plot Alpha");
            snapshot.PropertyName.ShouldBe("South Farm");
            snapshot.IsActive.ShouldBeTrue();
            snapshot.UpdatedAt.ShouldBeNull();
        }

        [Fact]
        public void Create_WithNullLabel_ShouldSucceed()
        {
            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: null,
                plotName: "Plot Alpha",
                propertyName: "South Farm");

            snapshot.Label.ShouldBeNull();
            snapshot.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Create_ShouldSetCreatedAtToUtcNow()
        {
            var before = DateTimeOffset.UtcNow;

            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Test",
                plotName: "Plot",
                propertyName: "Property");

            var after = DateTimeOffset.UtcNow;

            snapshot.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
            snapshot.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        }

        #endregion

        #region Create - With CreatedAt

        [Fact]
        public void Create_WithCreatedAt_ShouldUseProvidedTimestamp()
        {
            var occurredOn = DateTimeOffset.UtcNow.AddHours(-2);

            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Sensor East",
                plotName: "Plot Beta",
                propertyName: "North Farm",
                createdAt: occurredOn);

            snapshot.CreatedAt.ShouldBe(occurredOn);
            snapshot.IsActive.ShouldBeTrue();
            snapshot.UpdatedAt.ShouldBeNull();
        }

        #endregion

        #region Delete

        [Fact]
        public void Delete_ActiveSnapshot_ShouldSetInactive()
        {
            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Test",
                plotName: "Plot",
                propertyName: "Property");

            snapshot.Delete();

            snapshot.IsActive.ShouldBeFalse();
            snapshot.UpdatedAt.ShouldNotBeNull();
        }

        [Fact]
        public void Delete_AlreadyInactive_ShouldNotUpdateTimestamp()
        {
            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Test",
                plotName: "Plot",
                propertyName: "Property");

            snapshot.Delete();
            var firstUpdatedAt = snapshot.UpdatedAt;

            snapshot.Delete();

            snapshot.IsActive.ShouldBeFalse();
            snapshot.UpdatedAt.ShouldBe(firstUpdatedAt);
        }

        #endregion
    }
}
