namespace _123Vendas.Shared.Events;

/// <summary>
/// Classe base abstrata para eventos de domínio.
/// Implementa INotification do MediatR para comunicação assíncrona entre módulos.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    protected DomainEvent() { }

    protected DomainEvent(Guid eventId, DateTime occurredAt)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
    }
}
