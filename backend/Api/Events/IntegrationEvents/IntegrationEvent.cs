namespace Api.Events.IntegrationEvents
{
    public record IntegrationEvent
    {   // Nema potrebe da ovo bude interface, jer ne nasledjujem nista built-in kao Domain Event sto implementira MediatR interface
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime OccuredOn { get; init; } = DateTime.UtcNow;
        public string EventType => GetType().AssemblyQualifiedName; // Klasa u kojoj je implementiran interface tj CommentCreatedIntegrationEvent
        // Ovo ne treba seter da ima
    }
}
