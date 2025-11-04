using MediatR;

namespace _123Vendas.Shared.Events;

/// <summary>
/// Interface base para eventos de domínio.
/// Implementa INotification do MediatR para publicação assíncrona.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
