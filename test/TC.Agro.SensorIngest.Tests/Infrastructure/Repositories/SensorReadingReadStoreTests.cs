using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Domain.Snapshots;
using TC.Agro.SensorIngest.Infrastructure;
using TC.Agro.SensorIngest.Infrastructure.Repositories;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.SensorIngest.Tests.Infrastructure.Repositories;

public sealed class SensorReadingReadStoreTests
{
    [Fact]
    public async Task GetLatestReadingsAsync_WhenCallerIsProducer_ShouldReturnOnlyOwnOwnerData()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext, ct);

        var sut = new SensorReadingReadStore(dbContext, CreateProducerContext(seed.OwnerAId));

        var query = new GetLatestReadingsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var (readings, totalCount) = await sut.GetLatestReadingsAsync(query, ct);

        totalCount.ShouldBe(1);
        readings.Count.ShouldBe(1);
        readings[0].SensorId.ShouldBe(seed.OwnerASensorId);
    }

    [Fact]
    public async Task GetLatestReadingsAsync_WhenCallerIsAdminAndOwnerFilterIsProvided_ShouldApplyOwnerFilter()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext, ct);

        var sut = new SensorReadingReadStore(dbContext, CreateAdminContext());

        var query = new GetLatestReadingsQuery
        {
            OwnerId = seed.OwnerBId,
            PageNumber = 1,
            PageSize = 10
        };

        var (readings, totalCount) = await sut.GetLatestReadingsAsync(query, ct);

        totalCount.ShouldBe(1);
        readings.Count.ShouldBe(1);
        readings[0].SensorId.ShouldBe(seed.OwnerBSensorId);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"sensor-reading-read-store-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid OwnerAId, Guid OwnerBId, Guid OwnerASensorId, Guid OwnerBSensorId)> SeedDataAsync(
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        var ownerAId = Guid.NewGuid();
        var ownerBId = Guid.NewGuid();
        var ownerASensorId = Guid.NewGuid();
        var ownerBSensorId = Guid.NewGuid();

        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerAId, "Owner A", "owner-a@tcagro.test"));
        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerBId, "Owner B", "owner-b@tcagro.test"));

        dbContext.SensorSnapshots.Add(SensorSnapshot.Create(
            id: ownerASensorId,
            ownerId: ownerAId,
            propertyId: Guid.NewGuid(),
            plotId: Guid.NewGuid(),
            label: "Sensor A",
            plotName: "Plot A",
            propertyName: "Property A",
            status: "Active"));

        dbContext.SensorSnapshots.Add(SensorSnapshot.Create(
            id: ownerBSensorId,
            ownerId: ownerBId,
            propertyId: Guid.NewGuid(),
            plotId: Guid.NewGuid(),
            label: "Sensor B",
            plotName: "Plot B",
            propertyName: "Property B",
            status: "Active"));

        var ownerAReading = SensorReadingAggregate.Create(
            ownerASensorId,
            DateTime.UtcNow.AddMinutes(-5),
            temperature: 29,
            humidity: 60,
            soilMoisture: 44,
            rainfall: 0,
            batteryLevel: 80);

        var ownerBReading = SensorReadingAggregate.Create(
            ownerBSensorId,
            DateTime.UtcNow.AddMinutes(-3),
            temperature: 33,
            humidity: 52,
            soilMoisture: 36,
            rainfall: 0,
            batteryLevel: 75);

        ownerAReading.IsSuccess.ShouldBeTrue();
        ownerBReading.IsSuccess.ShouldBeTrue();

        dbContext.SensorReadings.Add(ownerAReading.Value);
        dbContext.SensorReadings.Add(ownerBReading.Value);

        await dbContext.SaveChangesAsync(ct);

        return (ownerAId, ownerBId, ownerASensorId, ownerBSensorId);
    }

    private static IUserContext CreateProducerContext(Guid ownerId)
    {
        var userContext = A.Fake<IUserContext>();

        A.CallTo(() => userContext.IsAdmin).Returns(false);
        A.CallTo(() => userContext.IsAuthenticated).Returns(true);
        A.CallTo(() => userContext.Id).Returns(ownerId);

        return userContext;
    }

    private static IUserContext CreateAdminContext()
    {
        var userContext = A.Fake<IUserContext>();

        A.CallTo(() => userContext.IsAdmin).Returns(true);
        A.CallTo(() => userContext.IsAuthenticated).Returns(true);
        A.CallTo(() => userContext.Id).Returns(Guid.NewGuid());

        return userContext;
    }
}
