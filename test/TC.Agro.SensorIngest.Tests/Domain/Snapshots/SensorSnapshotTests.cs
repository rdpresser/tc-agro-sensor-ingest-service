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

        #region Update

        [Fact]
        public void Update_WithValidData_ShouldUpdateAllFields()
        {
            // Arrange
            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Original Sensor",
                plotName: "Original Plot",
                propertyName: "Original Property");

            var newOwnerId = Guid.NewGuid();
            var newPropertyId = Guid.NewGuid();
            var newPlotId = Guid.NewGuid();
            // Act
            snapshot.Update(
                ownerId: newOwnerId,
                propertyId: newPropertyId,
                plotId: newPlotId,
                sensorName: "Updated Sensor",
                plotName: "Updated Plot",
                propertyName: "Updated Property",
                status: "Active",
                reason: null);

            // Assert
            snapshot.OwnerId.ShouldBe(newOwnerId);
            snapshot.PropertyId.ShouldBe(newPropertyId);
            snapshot.PlotId.ShouldBe(newPlotId);
            snapshot.Label.ShouldBe("Updated Sensor");
            snapshot.PlotName.ShouldBe("Updated Plot");
            snapshot.PropertyName.ShouldBe("Updated Property");
        }

        [Fact]
        public void Update_ShouldSetUpdatedAtToUtcNow()
        {
            // Arrange
            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Test",
                plotName: "Plot",
                propertyName: "Property");

            var before = DateTimeOffset.UtcNow;

            // Act
            snapshot.Update(
                ownerId: snapshot.OwnerId,
                propertyId: snapshot.PropertyId,
                plotId: snapshot.PlotId,
                sensorName: "Updated",
                plotName: "Updated Plot",
                propertyName: "Updated Property",
                status: "Active",
                reason: null);

            var after = DateTimeOffset.UtcNow;

            // Assert
            snapshot.UpdatedAt.ShouldNotBeNull();
            snapshot.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
            snapshot.UpdatedAt!.Value.ShouldBeLessThanOrEqualTo(after);
        }

        [Fact]
        public void Update_WithNullLabel_ShouldSetLabelToNull()
        {
            // Arrange
            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Original Sensor",
                plotName: "Plot",
                propertyName: "Property",
                status: "Active");

            // Act
            snapshot.Update(
                ownerId: snapshot.OwnerId,
                propertyId: snapshot.PropertyId,
                plotId: snapshot.PlotId,
                sensorName: null!,
                plotName: "Plot",
                propertyName: "Property",
                status: "Active",
                reason: null);

            // Assert
            snapshot.Label.ShouldBeNull();
        }

        [Fact]
        public void Update_CalledMultipleTimes_ShouldKeepMostRecentValues()
        {
            // Arrange
            var snapshot = SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Test",
                plotName: "Plot",
                propertyName: "Property");

            // First update
            snapshot.Update(
                ownerId: snapshot.OwnerId,
                propertyId: snapshot.PropertyId,
                plotId: snapshot.PlotId,
                sensorName: "First Update",
                plotName: "First Plot",
                propertyName: "First Property",
                status: "Active",
                reason: null);

            // Second update
            snapshot.Update(
                ownerId: snapshot.OwnerId,
                propertyId: snapshot.PropertyId,
                plotId: snapshot.PlotId,
                sensorName: "Second Update",
                plotName: "Second Plot",
                propertyName: "Second Property",
                status: "Inactive",
                reason: "Manual update");

            // Assert
            snapshot.UpdatedAt.ShouldNotBeNull();
            snapshot.Label.ShouldBe("Second Update");
            snapshot.PlotName.ShouldBe("Second Plot");
            snapshot.PropertyName.ShouldBe("Second Property");
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
