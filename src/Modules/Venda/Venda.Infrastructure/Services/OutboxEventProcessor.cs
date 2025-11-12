using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using _123Vendas.Shared.Events;
using Venda.Infrastructure.Entities;
using Venda.Infrastructure.Interfaces;
using Venda.Infrastructure.Models;

namespace Venda.Infrastructure.Services;

/// <summary>
/// Implementação do processador de eventos individuais do Outbox Pattern.
/// Responsável por desserializar eventos JSON e publicá-los via MediatR.
/// </summary>
public class OutboxEventProcessor : IOutboxEventProcessor
{
    private readonly IMediator _mediator;
    private readonly ILogger<OutboxEventProcessor> _logger;
    
    public OutboxEventProcessor(
        IMediator mediator,
        ILogger<OutboxEventProcessor> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Processa um evento individual do outbox.
    /// Desserializa o evento e o publica via MediatR.
    /// </summary>
    public async Task<ProcessingResult> ProcessAsync(
        OutboxEvent outboxEvent, 
        CancellationToken cancellationToken = default)
    {
        if (outboxEvent == null)
        {
            throw new ArgumentNullException(nameof(outboxEvent));
        }
        
        try
        {
            // Desserializar evento
            var evento = await DeserializarEventoAsync(outboxEvent);
            
            if (evento == null)
            {
                var errorMsg = $"Falha ao desserializar evento {outboxEvent.EventType}";
                _logger.LogWarning(
                    "{ErrorMessage}. EventId: {EventId}",
                    errorMsg,
                    outboxEvent.Id);
                
                return ProcessingResult.FailureResult(errorMsg);
            }
            
            // Publicar evento via MediatR
            await _mediator.Publish(evento, cancellationToken);
            
            _logger.LogInformation(
                "Evento {EventType} processado com sucesso. EventId: {EventId}",
                outboxEvent.EventType,
                outboxEvent.Id);
            
            return ProcessingResult.SuccessResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar evento {EventType}. EventId: {EventId}",
                outboxEvent.EventType,
                outboxEvent.Id);
            
            return ProcessingResult.FailureResult(ex.Message);
        }
    }
    
    /// <summary>
    /// Desserializa o evento JSON para o tipo apropriado.
    /// </summary>
    private async Task<INotification?> DeserializarEventoAsync(OutboxEvent outboxEvent)
    {
        return await Task.Run(() =>
        {
            var eventType = Type.GetType(outboxEvent.EventType);
            
            if (eventType == null)
            {
                _logger.LogWarning(
                    "Tipo de evento não encontrado: {EventType}. EventId: {EventId}",
                    outboxEvent.EventType,
                    outboxEvent.Id);
                
                return null;
            }
            
            var evento = JsonSerializer.Deserialize(outboxEvent.EventData, eventType) as INotification;
            
            return evento;
        });
    }
}
