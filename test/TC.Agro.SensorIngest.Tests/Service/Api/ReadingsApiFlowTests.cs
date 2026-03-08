using System.Net;
using System.Net.Http.Json;
using TC.Agro.SensorIngest.Application.UseCases.CreateReading;
using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;
using TC.Agro.SensorIngest.Tests.TestHelpers.Api;
using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.SensorIngest.Tests.Service.Api;

public sealed class ReadingsApiFlowTests : IClassFixture<SensorIngestApiWebApplicationFactory>
{
    private readonly SensorIngestApiWebApplicationFactory _factory;

    public ReadingsApiFlowTests(SensorIngestApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLatestReadings_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var ct = TestContext.Current.CancellationToken;

        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/readings/latest?pageNumber=1&pageSize=10", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReadingAndGetLatest_WithProducerRole_ShouldReturnPersistedReading()
    {
        var ct = TestContext.Current.CancellationToken;

        await _factory.ResetDatabaseAsync();

        var ownerId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();

        await _factory.SeedOwnerAndSensorAsync(ownerId, sensorId);

        using var client = _factory.CreateAuthenticatedClient("Producer", ownerId);

        var createResponse = await client.PostAsJsonAsync("/api/readings", new
        {
            SensorId = sensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 31.5,
            Humidity = 62.1,
            SoilMoisture = 40.0,
            Rainfall = 0.0,
            BatteryLevel = 88.0
        }, ct);

        createResponse.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);

        var createdPayload = await createResponse.Content.ReadFromJsonAsync<CreateReadingResponse>(cancellationToken: ct);
        createdPayload.ShouldNotBeNull();
        createdPayload!.SensorId.ShouldBe(sensorId);

        var latestResponse = await client.GetAsync($"/api/readings/latest?sensorId={sensorId}&pageNumber=1&pageSize=10", ct);

        latestResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var latestPayload = await latestResponse.Content.ReadFromJsonAsync<PaginatedResponse<GetLatestReadingsResponse>>(cancellationToken: ct);
        latestPayload.ShouldNotBeNull();
        latestPayload!.Data.Count.ShouldBe(1);
        latestPayload.Data[0].SensorId.ShouldBe(sensorId);
    }

    [Fact]
    public async Task CreateReading_WithoutAnyMetric_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        await _factory.ResetDatabaseAsync();

        var ownerId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        await _factory.SeedOwnerAndSensorAsync(ownerId, sensorId);

        using var client = _factory.CreateAuthenticatedClient("Producer", ownerId);

        var response = await client.PostAsJsonAsync("/api/readings", new
        {
            SensorId = sensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = (double?)null,
            Humidity = (double?)null,
            SoilMoisture = (double?)null,
            Rainfall = (double?)null,
            BatteryLevel = (double?)null
        }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync(ct);
        body.ShouldContain("errors", Case.Insensitive);
    }
}
