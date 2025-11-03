using _123Vendas.Shared.Events;
using Venda.Infrastructure.Entities;

namespace Venda.Infrastructure.Interfaces;

/// <summary>
/// Serviço para gerenciar eventos no padrão Outbox.
/// Garante consistência transacional entre persistência de dados e publicação de eventos.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Adiciona um evento de domínio ao outbox para processamento posterior.
    /// </summary>
    Task AdicionarEventoAsync(IDomainEvent evento, CancellationToken ct = default);
    
    /// <summary>
    /// Obtém eventos pendentes de processamento.
    /// </summary>
    Task<List<OutboxEvent>> ObterEventosPendentesAsync(int batchSize = 50, CancellationToken ct = default);
    
    /// <summary>
    /// Marca um evento como processado com sucesso.
    /// </summary>
    Task MarcarComoProcessadoAsync(Guid eventoId, CancellationToken ct = default);
    
    /// <summary>
    /// Marca um evento como falhado e incrementa contador de retry.
    /// </summary>
    Task MarcarComoFalhadoAsync(Guid eventoId, string erro, CancellationToken ct = default);
}
