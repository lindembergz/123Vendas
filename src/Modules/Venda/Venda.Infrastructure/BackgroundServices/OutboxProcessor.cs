using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Venda.Infrastructure.Configuration;
using Venda.Infrastructure.Interfaces;

namespace Venda.Infrastructure.BackgroundServices;

/// <summary>
/// Background service que processa eventos do Outbox Pattern.
/// Executa periodicamente (configurável via appsettings.json) e publica eventos pendentes via MediatR.
/// Implementa retry automático com limite de 5 tentativas.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxProcessorSettings _settings;
    
    public OutboxProcessor(
        IServiceProvider serviceProvider, 
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxProcessorSettings> settings)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxProcessor iniciado. Intervalo: {ProcessingInterval}s, Batch: {BatchSize}, ErrorDelay: {ErrorDelay}s",
            _settings.ProcessingIntervalSeconds,
            _settings.BatchSize,
            _settings.ErrorDelaySeconds);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarEventosPendentesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Cancelamento normal, não logar como erro
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no OutboxProcessor");
                await Task.Delay(TimeSpan.FromSeconds(_settings.ErrorDelaySeconds), stoppingToken);
            }
        }
        
        _logger.LogInformation("OutboxProcessor finalizado");
    }
    
    /// <summary>
    /// Processa eventos pendentes do outbox em lote.
    /// </summary>
    private async Task ProcessarEventosPendentesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var eventProcessor = scope.ServiceProvider.GetRequiredService<IOutboxEventProcessor>();
        
        var eventosPendentes = await outbox.ObterEventosPendentesAsync(_settings.BatchSize, stoppingToken);
        
        if (eventosPendentes.Count == 0)
        {
            return;
        }
        
        _logger.LogInformation("Processando {Count} eventos pendentes", eventosPendentes.Count);
        
        await ProcessarLoteDeEventosAsync(eventosPendentes, outbox, eventProcessor, stoppingToken);
    }
    
    /// <summary>
    /// Processa um lote de eventos do outbox.
    /// </summary>
    private async Task ProcessarLoteDeEventosAsync(
        IReadOnlyList<Entities.OutboxEvent> eventos,
        IOutboxService outbox,
        IOutboxEventProcessor eventProcessor,
        CancellationToken stoppingToken)
    {
        foreach (var outboxEvent in eventos)
        {
            await ProcessarEventoIndividualAsync(outboxEvent, outbox, eventProcessor, stoppingToken);
        }
    }
    
    /// <summary>
    /// Processa um evento individual do outbox.
    /// </summary>
    private async Task ProcessarEventoIndividualAsync(
        Entities.OutboxEvent outboxEvent,
        IOutboxService outbox,
        IOutboxEventProcessor eventProcessor,
        CancellationToken stoppingToken)
    {
        try
        {
            var result = await eventProcessor.ProcessAsync(outboxEvent, stoppingToken);
            
            if (result.Success)
            {
                await outbox.MarcarComoProcessadoAsync(outboxEvent.Id, stoppingToken);
            }
            else
            {
                await outbox.MarcarComoFalhadoAsync(
                    outboxEvent.Id,
                    result.ErrorMessage ?? "Erro desconhecido",
                    stoppingToken);
            }
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
