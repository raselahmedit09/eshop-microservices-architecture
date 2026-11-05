namespace Ordering.Domain.Abstractions
{
    public abstract record DomainEvent : IDomainEvent
    {
        // Assigned once when the event is created, so every read returns the same value
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.Now;
        public string EventType => GetType().AssemblyQualifiedName!;
    }
}
