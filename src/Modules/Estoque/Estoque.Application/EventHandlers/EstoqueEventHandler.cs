using _123Vendas.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Estoque.Application.EventHandlers;

/// <summary>
/// Handler de eventos de domínio para o módulo de Estoque.
/// Simula ajustes e reversões de estoque baseados em eventos de vendas.
/// Implementa graceful degradation com try-catch para não bloquear o fluxo principal.
/// </summary>
public class EstoqueEventHandler :
    INotificationHandler<CompraCriada>,
    INotificationHandler<CompraCancelada>
{
    private readonly ILogger<EstoqueEventHandler> _logger;

    public EstoqueEventHandler(ILogger<EstoqueEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa evento de compra criada, simulando ajuste de estoque.
    /// </summary>
    public async Task Handle(CompraCriada notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Evento {EventType} recebido: Venda {VendaId} (Número {NumeroVenda}) - Ajustando estoque para cliente {ClienteId}",
                nameof(CompraCriada),
                notification.VendaId,
                notification.NumeroVenda,
                notification.ClienteId);

            // Simulação: em produção, chamaria serviço de estoque real
            // Exemplo: await _estoqueService.AjustarEstoqueAsync(notification.VendaId, cancellationToken);
            
            await Task.CompletedTask;

            _logger.LogInformation(
                "Estoque ajustado com sucesso para venda {VendaId}",
                notification.VendaId);
        }
        catch (Exception ex)
        {
            // Graceful degradation: loga erro mas não propaga exceção
            _logger.LogError(
                ex,
                "Erro ao processar ajuste de estoque para venda {VendaId}. Operação continuará sem bloquear o fluxo principal.",
                notification.VendaId);
        }
    }

    /// <summary>
    /// Processa evento de compra cancelada, simulando reversão de estoque.
    /// </summary>
    public async Task Handle(CompraCancelada notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Evento {EventType} recebido: Venda {VendaId} - Revertendo estoque. Motivo: {Motivo}",
                nameof(CompraCancelada),
                notification.VendaId,
                notification.Motivo);

            // Simulação: em produção, chamaria serviço de estoque real
            // Exemplo: await _estoqueService.ReverterEstoqueAsync(notification.VendaId, cancellationToken);
            
            await Task.CompletedTask;

            _logger.LogInformation(
                "Estoque revertido com sucesso para venda {VendaId}",
                notification.VendaId);
        }
        catch (Exception ex)
        {
            // Graceful degradation: loga erro mas não propaga exceção
            _logger.LogError(
                ex,
                "Erro ao processar reversão de estoque para venda {VendaId}. Operação continuará sem bloquear o fluxo principal.",
                notification.VendaId);
        }
    }
}
