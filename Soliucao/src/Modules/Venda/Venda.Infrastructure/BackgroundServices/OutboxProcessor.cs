using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using _123Vendas.Shared.Events;
using Venda.Infrastructure.Interfaces;

namespace Venda.Infrastructure.BackgroundServices;

/// <summary>
/// Background service que processa eventos do Outbox Pattern.
/// Executa a cada 10 segundos e publica eventos pendentes via MediatR.
/// Implementa retry automático com limite de 5 tentativas.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private const int ProcessingIntervalSeconds = 10;
    private const int ErrorDelaySeconds = 30;
    
    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor iniciado");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarEventosPendentesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Cancelamento normal, não logar como erro
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no OutboxProcessor");
                await Task.Delay(TimeSpan.FromSeconds(ErrorDelaySeconds), stoppingToken);
            }
        }
        
        _logger.LogInformation("OutboxProcessor finalizado");
    }
    
    private async Task ProcessarEventosPendentesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var eventosPendentes = await outbox.ObterEventosPendentesAsync(50, stoppingToken);
        
        if (eventosPendentes.Count == 0)
        {
            return;
        }
        
        _logger.LogInformation("Processando {Count} eventos pendentes", eventosPendentes.Count);
        
        foreach (var outboxEvent in eventosPendentes)
        {
            try
            {
                // Deserializar evento
                var eventType = Type.GetType(outboxEvent.EventType);
                
                if (eventType == null)
                {
                    _logger.LogWarning(
                        "Tipo de evento não encontrado: {EventType}. EventId: {EventId}",
                        outboxEvent.EventType,
                        outboxEvent.Id);
                    
                    await outbox.MarcarComoFalhadoAsync(
                        outboxEvent.Id,
                        $"Tipo de evento não encontrado: {outboxEvent.EventType}",
                        stoppingToken);
                    
                    continue;
                }
                
                var evento = JsonSerializer.Deserialize(outboxEvent.EventData, eventType) as INotification;
                
                if (evento == null)
                {
                    _logger.LogWarning(
                        "Falha ao desserializar evento {EventType}. EventId: {EventId}",
                        outboxEvent.EventType,
                        outboxEvent.Id);
                    
                    await outbox.MarcarComoFalhadoAsync(
                        outboxEvent.Id,
                        "Falha ao desserializar evento",
                        stoppingToken);
                    
                    continue;
                }
                
                // Publicar evento via MediatR
                await mediator.Publish(evento, stoppingToken);
                
                // Marcar como processado
                await outbox.MarcarComoProcessadoAsync(outboxEvent.Id, stoppingToken);
                
                _logger.LogInformation(
                    "Evento {EventType} processado com sucesso. EventId: {EventId}",
                    outboxEvent.EventType,
                    outboxEvent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar evento {EventType}. EventId: {EventId}. Tentativa: {RetryCount}",
                    outboxEvent.EventType,
                    outboxEvent.Id,
                    outboxEvent.RetryCount + 1);
                
                await outbox.MarcarComoFalhadoAsync(outboxEvent.Id, ex.Message, stoppingToken);
            }
        }
    }
}
