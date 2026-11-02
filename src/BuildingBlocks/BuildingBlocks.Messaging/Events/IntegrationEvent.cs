namespace BuildingBlocks.Messaging.Events
{
    public record IntegrationEvent
    {
        // Assigned once when the event is created; init lets the consumer deserialize the publisher's values.
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.Now;
        public string EventType => GetType().AssemblyQualifiedName;
    }
}
