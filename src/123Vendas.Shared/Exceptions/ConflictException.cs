namespace _123Vendas.Shared.Exceptions;

/// <summary>
/// Exceção lançada quando há um conflito de estado ou operação inválida.
/// Representa tentativas de operações que violam o estado atual do recurso.
/// Mapeada para HTTP 409 Conflict.
/// </summary>
public class ConflictException : Exception
{
    /// <summary>
    /// Código de erro opcional para identificação específica do conflito.
    /// </summary>
    public string? ErrorCode { get; }

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConflictException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
