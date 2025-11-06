namespace Venda.Infrastructure.Configuration;

/// <summary>
/// Configurações do OutboxProcessor para processamento de eventos.
/// </summary>
public class OutboxProcessorSettings
{
    /// <summary>
    /// Intervalo em segundos entre cada execução do processamento de eventos.
    /// Padrão: 10 segundos.
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 10;
    
    /// <summary>
    /// Intervalo em segundos de espera após um erro no processamento.
    /// Padrão: 30 segundos.
    /// </summary>
    public int ErrorDelaySeconds { get; set; } = 30;
    
    /// <summary>
    /// Número máximo de eventos a serem processados por lote.
    /// Padrão: 50 eventos.
    /// </summary>
    public int BatchSize { get; set; } = 50;
}
