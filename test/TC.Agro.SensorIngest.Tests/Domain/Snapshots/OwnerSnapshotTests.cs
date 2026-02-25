using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Tests.Domain.Snapshots
{
    public class OwnerSnapshotTests
    {
        #region Create - Without CreatedAt

        [Fact]
        public void Create_WithValidData_ShouldSetAllProperties()
        {
            var id = Guid.NewGuid();
            var name = "John Doe";
            var email = "john@example.com";

            var snapshot = OwnerSnapshot.Create(id, name, email);

            snapshot.Id.ShouldBe(id);
            snapshot.Name.ShouldBe(name);
            snapshot.Email.ShouldBe(email);
            snapshot.IsActive.ShouldBeTrue();
            snapshot.UpdatedAt.ShouldBeNull();
        }

        [Fact]
        public void Create_ShouldSetCreatedAtToUtcNow()
        {
            var before = DateTimeOffset.UtcNow;

            var snapshot = OwnerSnapshot.Create(Guid.NewGuid(), "Name", "email@test.com");

            var after = DateTimeOffset.UtcNow;

            snapshot.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
            snapshot.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        }

        [Fact]
        public void Create_ShouldSetIsActiveTrue()
        {
            var snapshot = OwnerSnapshot.Create(Guid.NewGuid(), "Name", "email@test.com");

            snapshot.IsActive.ShouldBeTrue();
        }

        #endregion

        #region Create - With CreatedAt

        [Fact]
        public void Create_WithCreatedAt_ShouldUseProvidedTimestamp()
        {
            var occurredOn = DateTimeOffset.UtcNow.AddHours(-3);

            var snapshot = OwnerSnapshot.Create(
                Guid.NewGuid(), "Name", "email@test.com", occurredOn);

            snapshot.CreatedAt.ShouldBe(occurredOn);
            snapshot.IsActive.ShouldBeTrue();
            snapshot.UpdatedAt.ShouldBeNull();
        }

        #endregion

        #region Update

        [Fact]
        public void Update_ShouldChangeNameAndEmail()
        {
            var snapshot = OwnerSnapshot.Create(Guid.NewGuid(), "Old Name", "old@test.com");

            snapshot.Update("New Name", "new@test.com", isActive: true);

            snapshot.Name.ShouldBe("New Name");
            snapshot.Email.ShouldBe("new@test.com");
        }

        [Fact]
        public void Update_ShouldSetUpdatedAtToUtcNow()
        {
            var snapshot = OwnerSnapshot.Create(Guid.NewGuid(), "Name", "email@test.com");

            var before = DateTimeOffset.UtcNow;
            snapshot.Update("Updated", "updated@test.com", isActive: true);
            var after = DateTimeOffset.UtcNow;

            snapshot.UpdatedAt.ShouldNotBeNull();
            snapshot.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
            snapshot.UpdatedAt!.Value.ShouldBeLessThanOrEqualTo(after);
        }

        [Fact]
        public void Update_ShouldPreserveId()
        {
            var id = Guid.NewGuid();
            var snapshot = OwnerSnapshot.Create(id, "Name", "email@test.com");

            snapshot.Update("New", "new@test.com", isActive: true);

            snapshot.Id.ShouldBe(id);
        }

        [Fact]
        public void Update_WithEmptyNameAndEmail_ShouldStillUpdate()
        {
            var snapshot = OwnerSnapshot.Create(Guid.NewGuid(), "Name", "email@test.com");

            snapshot.Update("", "", isActive: true);

            snapshot.Name.ShouldBe("");
            snapshot.Email.ShouldBe("");
        }

        #endregion

        #region Delete

        [Fact]
        public void Delete_ActiveSnapshot_ShouldSetIsActiveFalse()
        {
            var snapshot = OwnerSnapshot.Create(Guid.NewGuid(), "Name", "email@test.com");

            snapshot.Delete();

            snapshot.IsActive.ShouldBeFalse();
            snapshot.UpdatedAt.ShouldNotBeNull();
        }

        [Fact]
        public void Delete_AlreadyInactive_ShouldNotUpdateTimestamp()
        {
            var snapshot = OwnerSnapshot.Create(Guid.NewGuid(), "Name", "email@test.com");

            snapshot.Delete();
            var firstUpdatedAt = snapshot.UpdatedAt;

            // Call Delete again - should be idempotent
            snapshot.Delete();

            snapshot.IsActive.ShouldBeFalse();
            snapshot.UpdatedAt.ShouldBe(firstUpdatedAt);
        }

        #endregion
    }
}
