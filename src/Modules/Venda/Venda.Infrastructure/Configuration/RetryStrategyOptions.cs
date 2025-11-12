namespace Venda.Infrastructure.Configuration;

/// <summary>
/// Opções de configuração para a estratégia de retry.
/// </summary>
public class RetryStrategyOptions
{
    /// <summary>
    /// Número máximo de tentativas de retry.
    /// </summary>
    public int MaxRetries { get; set; } = 5;
    
    /// <summary>
    /// Delay inicial em milissegundos antes do primeiro retry.
    /// </summary>
    public int InitialDelayMs { get; set; } = 50;
}
