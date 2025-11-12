using Venda.Infrastructure.Entities;
using Venda.Infrastructure.Models;

namespace Venda.Infrastructure.Interfaces;

/// <summary>
/// Interface para processamento de eventos individuais do Outbox Pattern.
/// Responsável por desserializar e publicar eventos via MediatR.
/// </summary>
public interface IOutboxEventProcessor
{
    /// <summary>
    /// Processa um evento individual do outbox.
    /// </summary>
    /// <param name="outboxEvent">Evento a ser processado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado do processamento indicando sucesso ou falha</returns>
    Task<ProcessingResult> ProcessAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
