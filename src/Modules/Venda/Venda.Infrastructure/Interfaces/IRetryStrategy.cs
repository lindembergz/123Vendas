namespace Venda.Infrastructure.Interfaces;

/// <summary>
/// Define uma estratégia de retry para operações que podem falhar temporariamente.
/// </summary>
public interface IRetryStrategy
{
    /// <summary>
    /// Executa uma operação com retry automático em caso de falha.
    /// </summary>
    /// <typeparam name="T">Tipo de retorno da operação</typeparam>
    /// <param name="operation">Operação a ser executada</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Resultado da operação</returns>
    /// <exception cref="InvalidOperationException">Quando o número máximo de tentativas é excedido</exception>
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
}
