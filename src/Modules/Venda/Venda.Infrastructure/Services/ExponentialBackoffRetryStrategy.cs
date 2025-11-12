using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Venda.Infrastructure.Configuration;
using Venda.Infrastructure.Interfaces;

namespace Venda.Infrastructure.Services;

/// <summary>
/// Implementa uma estratégia de retry com backoff exponencial.
/// Utilizada para operações que podem falhar temporariamente devido a conflitos de concorrência.
/// </summary>
public class ExponentialBackoffRetryStrategy : IRetryStrategy
{
    private readonly RetryStrategyOptions _options;
    private readonly ILogger<ExponentialBackoffRetryStrategy> _logger;
    
    /// <summary>
    /// Inicializa uma nova instância de <see cref="ExponentialBackoffRetryStrategy"/>.
    /// </summary>
    /// <param name="options">Opções de configuração do retry</param>
    /// <param name="logger">Logger para registrar tentativas de retry</param>
    public ExponentialBackoffRetryStrategy(
        IOptions<RetryStrategyOptions> options,
        ILogger<ExponentialBackoffRetryStrategy> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Executa uma operação com retry automático usando backoff exponencial.
    /// Trata especificamente DbUpdateException e DbUpdateConcurrencyException.
    /// Delays: 50ms, 100ms, 200ms, 400ms, 800ms (para 5 retries com delay inicial de 50ms).
    /// </summary>
    /// <typeparam name="T">Tipo de retorno da operação</typeparam>
    /// <param name="operation">Operação a ser executada</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Resultado da operação</returns>
    /// <exception cref="InvalidOperationException">Quando o número máximo de tentativas é excedido</exception>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
    {
        var retryCount = 0;
        
        while (retryCount < _options.MaxRetries)
        {
            try
            {
                return await operation();
            }
            catch (DbUpdateException ex) when (IsRetriableException(ex))
            {
                retryCount++;
                
                if (retryCount >= _options.MaxRetries)
                {
                    _logger.LogError(ex, 
                        "Falha ao executar operação após {MaxRetries} tentativas", 
                        _options.MaxRetries);
                    throw new InvalidOperationException(
                        $"Falha ao executar operação após {_options.MaxRetries} tentativas devido a conflito de concorrência.", 
                        ex);
                }
                
                var delayMs = _options.InitialDelayMs * (int)Math.Pow(2, retryCount - 1);
                
                _logger.LogWarning(
                    "Conflito de concorrência detectado. Tentativa {RetryCount}/{MaxRetries}. Aguardando {DelayMs}ms antes de tentar novamente.",
                    retryCount, _options.MaxRetries, delayMs);
                
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                
                if (retryCount >= _options.MaxRetries)
                {
                    _logger.LogError(ex, 
                        "Falha ao executar operação após {MaxRetries} tentativas", 
                        _options.MaxRetries);
                    throw new InvalidOperationException(
                        $"Falha ao executar operação após {_options.MaxRetries} tentativas devido a conflito de concorrência.", 
                        ex);
                }
                
                var delayMs = _options.InitialDelayMs * (int)Math.Pow(2, retryCount - 1);
                
                _logger.LogWarning(
                    "Conflito de concorrência detectado. Tentativa {RetryCount}/{MaxRetries}. Aguardando {DelayMs}ms antes de tentar novamente.",
                    retryCount, _options.MaxRetries, delayMs);
                
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct);
            }
        }
        
        throw new InvalidOperationException(
            $"Falha inesperada ao executar operação após {_options.MaxRetries} tentativas.");
    }
    
    /// <summary>
    /// Verifica se a exceção é retriável (violação de constraint única).
    /// </summary>
    private static bool IsRetriableException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true 
               || ex.InnerException?.Message.Contains("IX_Vendas_FilialId_NumeroVenda_Unique") == true;
    }
}
