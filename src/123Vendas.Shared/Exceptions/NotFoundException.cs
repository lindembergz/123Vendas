namespace _123Vendas.Shared.Exceptions;

/// <summary>
/// Exceção lançada quando um recurso solicitado não é encontrado.
/// Mapeada para HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Nome da entidade que não foi encontrada.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Chave/ID do recurso que não foi encontrado.
    /// </summary>
    public object Key { get; }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} com ID '{key}' não foi encontrado.")
    {
        EntityName = entityName;
        Key = key;
    }

    public NotFoundException(string entityName, object key, string message)
        : base(message)
    {
        EntityName = entityName;
        Key = key;
    }

    public NotFoundException(string message) : base(message)
    {
        EntityName = string.Empty;
        Key = string.Empty;
    }
}
