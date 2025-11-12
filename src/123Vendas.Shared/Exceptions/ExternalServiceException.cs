namespace _123Vendas.Shared.Exceptions;

/// <summary>
/// Exceção lançada quando há falha na comunicação com serviços externos.
/// Representa erros de integração com APIs externas (CRM, Produto, etc).
/// Mapeada para HTTP 502 Bad Gateway.
/// </summary>
public class ExternalServiceException : Exception
{
    /// <summary>
    /// Nome do serviço externo que falhou.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Código de erro retornado pelo serviço externo (opcional).
    /// </summary>
    public string? ErrorCode { get; }

    public ExternalServiceException(string serviceName, string message)
        : base($"Falha ao comunicar com o serviço '{serviceName}': {message}")
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base($"Falha ao comunicar com o serviço '{serviceName}': {message}", innerException)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, string errorCode)
        : base($"Falha ao comunicar com o serviço '{serviceName}': {message}")
    {
        ServiceName = serviceName;
        ErrorCode = errorCode;
    }

    public ExternalServiceException(string serviceName, string message, string errorCode, Exception innerException)
        : base($"Falha ao comunicar com o serviço '{serviceName}': {message}", innerException)
    {
        ServiceName = serviceName;
        ErrorCode = errorCode;
    }
}
