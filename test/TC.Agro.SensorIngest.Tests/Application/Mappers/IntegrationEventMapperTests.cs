using FakeItEasy;
using TC.Agro.Contracts.Events.SensorIngested;
using TC.Agro.SensorIngest.Application.Abstractions.Mappers;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SharedKernel.Domain.Events;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.SensorIngest.Tests.Application.Mappers
{
    public class IntegrationEventMapperTests
    {
        private readonly IUserContext _userContext;

        public IntegrationEventMapperTests()
        {
            _userContext = A.Fake<IUserContext>();
            A.CallTo(() => _userContext.Id).Returns(Guid.NewGuid());
            A.CallTo(() => _userContext.IsAuthenticated).Returns(true);
            A.CallTo(() => _userContext.CorrelationId).Returns(Guid.NewGuid().ToString());
        }

        private static SensorReadingAggregate CreateValidAggregate()
        {
            var result = SensorReadingAggregate.Create(
                sensorId: Guid.NewGuid(),
                time: DateTime.UtcNow.AddMinutes(-1),
                temperature: 25.0,
                humidity: 60.0,
                soilMoisture: null,
                rainfall: null,
                batteryLevel: null);
            return result.Value;
        }

        private static Dictionary<Type, Func<BaseDomainEvent, SensorIngestedIntegrationEvent>> CreateStandardMappings()
        {
            return new Dictionary<Type, Func<BaseDomainEvent, SensorIngestedIntegrationEvent>>
            {
                {
                    typeof(SensorReadingAggregate.SensorReadingCreatedDomainEvent),
                    e =>
                    {
                        var de = (SensorReadingAggregate.SensorReadingCreatedDomainEvent)e;
                        return new SensorIngestedIntegrationEvent(
                            de.SensorReadingId, de.SensorId, de.Time,
                            de.Temperature, de.Humidity, de.SoilMoisture,
                            de.Rainfall, de.BatteryLevel, de.OccurredOn);
                    }
                }
            };
        }

        #region Null Mappings

        [Fact]
        public void MapToIntegrationEvents_WithNullMappings_ShouldReturnEmpty()
        {
            var aggregate = CreateValidAggregate();

            var results = aggregate.UncommittedEvents.MapToIntegrationEvents<SensorReadingAggregate, SensorIngestedIntegrationEvent>(
                aggregate, _userContext, "TestHandler", mappings: null);

            results.ShouldBeEmpty();
        }

        #endregion

        #region Event Mapping

        [Fact]
        public void MapToIntegrationEvents_WithValidMapping_ShouldMapDomainEvent()
        {
            var aggregate = CreateValidAggregate();

            var results = aggregate.UncommittedEvents.MapToIntegrationEvents(
                aggregate, _userContext, "TestHandler", CreateStandardMappings()).ToList();

            results.Count.ShouldBe(1);
            results[0].EventData.SensorId.ShouldBe(aggregate.SensorId);
        }

        [Fact]
        public void MapToIntegrationEvents_ShouldPreserveAggregateId()
        {
            var aggregate = CreateValidAggregate();

            var results = aggregate.UncommittedEvents.MapToIntegrationEvents(
                aggregate, _userContext, "TestHandler", CreateStandardMappings()).ToList();

            results[0].EventData.SensorReadingId.ShouldBe(aggregate.Id);
        }

        #endregion

        #region Unmapped Events

        [Fact]
        public void MapToIntegrationEvents_WithNoMatchingMapping_ShouldSkipEvent()
        {
            var aggregate = CreateValidAggregate();

            var emptyMappings = new Dictionary<Type, Func<BaseDomainEvent, SensorIngestedIntegrationEvent>>();

            var results = aggregate.UncommittedEvents.MapToIntegrationEvents(
                aggregate, _userContext, "TestHandler", emptyMappings).ToList();

            results.ShouldBeEmpty();
        }

        #endregion

        #region Source Generation

        [Fact]
        public void MapToIntegrationEvents_WithHandlerName_ShouldSetCorrectSource()
        {
            var aggregate = CreateValidAggregate();

            var results = aggregate.UncommittedEvents.MapToIntegrationEvents(
                aggregate, _userContext, "CreateReadingHandler", CreateStandardMappings()).ToList();

            results[0].Source!.ShouldContain("CreateReadingHandler");
            results[0].Source!.ShouldContain("SensorIngestedIntegrationEvent");
        }

        [Fact]
        public void MapToIntegrationEvents_WithNullHandlerName_ShouldUseUnknownHandler()
        {
            var aggregate = CreateValidAggregate();

            var results = aggregate.UncommittedEvents.MapToIntegrationEvents(
                aggregate, _userContext, handlerName: null, CreateStandardMappings()).ToList();

            results[0].Source!.ShouldContain("UnknownHandler");
        }

        #endregion
    }
}
