namespace _123Vendas.Shared.Interfaces;

/// <summary>
/// Serviço para integração com o módulo de CRM para validação de clientes
/// </summary>
public interface IClienteService
{
    /// <summary>
    /// Verifica se um cliente existe no sistema de CRM
    /// </summary>
    /// <param name="clienteId">ID do cliente a ser verificado</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>True se o cliente existe, False caso contrário</returns>
    Task<bool> ClienteExisteAsync(Guid clienteId, CancellationToken ct = default);
}
