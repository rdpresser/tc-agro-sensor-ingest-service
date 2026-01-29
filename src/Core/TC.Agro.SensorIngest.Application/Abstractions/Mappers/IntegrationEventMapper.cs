namespace TC.Agro.SensorIngest.Application.Abstractions.Mappers
{
    public static class IntegrationEventMapper
    {
        public static IEnumerable<TIntegrationEvent> MapToIntegrationEvents<TAggregate, TIntegrationEvent>(
            this IEnumerable<BaseDomainEvent> domainEvents,
            TAggregate aggregate,
            IUserContext userContext,
            string handlerName,
            Dictionary<Type, Func<BaseDomainEvent, TIntegrationEvent>> mappings)
            where TAggregate : BaseAggregateRoot
            where TIntegrationEvent : class
        {
            foreach (var domainEvent in domainEvents)
            {
                var eventType = domainEvent.GetType();
                if (mappings.TryGetValue(eventType, out var mapper))
                {
                    yield return mapper(domainEvent);
                }
            }
        }
    }
}
