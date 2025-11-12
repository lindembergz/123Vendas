namespace _123Vendas.Shared.Exceptions;

/// <summary>
/// Exceção base para erros de domínio.
/// Representa violações de regras de negócio ou validações de entrada.
/// Mapeada para HTTP 422 Unprocessable Entity.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Código de erro opcional para identificação específica.
    /// </summary>
    public string? ErrorCode { get; }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public DomainException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
