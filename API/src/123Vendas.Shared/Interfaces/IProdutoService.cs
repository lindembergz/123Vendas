namespace _123Vendas.Shared.Interfaces;

/// <summary>
/// Serviço para integração com o módulo de Estoque para reserva de produtos
/// </summary>
public interface IProdutoService
{
    /// <summary>
    /// Reserva estoque para um produto específico
    /// </summary>
    /// <param name="produtoId">ID do produto</param>
    /// <param name="quantidade">Quantidade a ser reservada</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>True se a reserva foi bem-sucedida, False caso contrário</returns>
    Task<bool> ReservarEstoqueAsync(Guid produtoId, int quantidade, CancellationToken ct = default);
}
