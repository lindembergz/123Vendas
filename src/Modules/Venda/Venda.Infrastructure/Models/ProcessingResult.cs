namespace Venda.Infrastructure.Models;

/// <summary>
/// Representa o resultado do processamento de um evento do outbox.
/// </summary>
/// <param name="Success">Indica se o processamento foi bem-sucedido</param>
/// <param name="ErrorMessage">Mensagem de erro em caso de falha (opcional)</param>
public record ProcessingResult(bool Success, string? ErrorMessage = null)
{
    /// <summary>
    /// Cria um resultado de sucesso.
    /// </summary>
    public static ProcessingResult SuccessResult() => new(true);
    
    /// <summary>
    /// Cria um resultado de falha com mensagem de erro.
    /// </summary>
    /// <param name="errorMessage">Mensagem descrevendo o erro</param>
    public static ProcessingResult FailureResult(string errorMessage) => new(false, errorMessage);
}
