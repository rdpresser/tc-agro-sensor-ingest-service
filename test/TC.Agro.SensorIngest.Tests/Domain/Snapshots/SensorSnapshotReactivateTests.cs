using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Tests.Domain.Snapshots
{
    public class SensorSnapshotReactivateTests
    {
        private static SensorSnapshot CreateActiveSnapshot() =>
            SensorSnapshot.Create(
                id: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                label: "Test Sensor",
                plotName: "Plot Alpha",
                propertyName: "Farm South",
                changedByUserId: Guid.NewGuid());

        private static SensorSnapshot CreateInactiveSnapshot()
        {
            var snapshot = CreateActiveSnapshot();
            snapshot.Delete();
            return snapshot;
        }

        #region Reactivate

        [Fact]
        public void Reactivate_InactiveSnapshot_ShouldSetActive()
        {
            var snapshot = CreateInactiveSnapshot();

            snapshot.Reactivate();

            snapshot.IsActive.ShouldBeTrue();
            snapshot.UpdatedAt.ShouldNotBeNull();
        }

        [Fact]
        public void Reactivate_InactiveSnapshot_ShouldUpdateTimestamp()
        {
            var snapshot = CreateInactiveSnapshot();
            var beforeReactivate = DateTimeOffset.UtcNow;

            snapshot.Reactivate();

            snapshot.UpdatedAt.ShouldNotBeNull();
            snapshot.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeReactivate);
        }

        [Fact]
        public void Reactivate_AlreadyActive_ShouldNotUpdateTimestamp()
        {
            var snapshot = CreateActiveSnapshot();
            var originalUpdatedAt = snapshot.UpdatedAt;

            snapshot.Reactivate();

            snapshot.IsActive.ShouldBeTrue();
            snapshot.UpdatedAt.ShouldBe(originalUpdatedAt);
        }

        [Fact]
        public void Reactivate_AfterDelete_ShouldRestoreActiveState()
        {
            var snapshot = CreateActiveSnapshot();

            snapshot.Delete();
            snapshot.IsActive.ShouldBeFalse();

            snapshot.Reactivate();
            snapshot.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Reactivate_MultipleTimes_ShouldBeIdempotent()
        {
            var snapshot = CreateInactiveSnapshot();

            snapshot.Reactivate();
            var firstUpdatedAt = snapshot.UpdatedAt;

            snapshot.Reactivate();

            snapshot.IsActive.ShouldBeTrue();
            snapshot.UpdatedAt.ShouldBe(firstUpdatedAt);
        }

        #endregion
    }
}
