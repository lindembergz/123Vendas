using _123Vendas.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CRM.Application.EventHandlers;

/// <summary>
/// Handler de eventos de domínio para o módulo de CRM.
/// Simula atualização de histórico de cliente baseado em eventos de vendas.
/// Implementa graceful degradation com try-catch para não bloquear o fluxo principal.
/// </summary>
public class CrmEventHandler : INotificationHandler<CompraCriada>
{
    private readonly ILogger<CrmEventHandler> _logger;

    public CrmEventHandler(ILogger<CrmEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa evento de compra criada, simulando atualização de histórico do cliente.
    /// </summary>
    public async Task Handle(CompraCriada notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Evento {EventType} recebido: Cliente {ClienteId} - Atualizando histórico de compras. Venda {VendaId} (Número {NumeroVenda})",
                nameof(CompraCriada),
                notification.ClienteId,
                notification.VendaId,
                notification.NumeroVenda);

            // Simulação: em produção, chamaria serviço de CRM real
            // Exemplo: await _crmService.AtualizarHistoricoClienteAsync(notification.ClienteId, notification.VendaId, cancellationToken);
            
            await Task.CompletedTask;

            _logger.LogInformation(
                "Histórico do cliente {ClienteId} atualizado com sucesso",
                notification.ClienteId);
        }
        catch (Exception ex)
        {
            // Graceful degradation: loga erro mas não propaga exceção
            _logger.LogError(
                ex,
                "Erro ao processar atualização de histórico para cliente {ClienteId}. Operação continuará sem bloquear o fluxo principal.",
                notification.ClienteId);
        }
    }
}
