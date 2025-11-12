namespace Venda.Application.DTOs;

/// <summary>
/// DTO otimizado para listagem de vendas com projection direta do banco.
/// Contém apenas os campos necessários para exibição em listas, reduzindo tráfego de dados.
/// </summary>
public record VendaListDto
{
    /// <summary>
    /// Identificador único da venda.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Número sequencial da venda na filial.
    /// </summary>
    public int NumeroVenda { get; init; }
    
    /// <summary>
    /// Data de criação da venda.
    /// </summary>
    public DateTime Data { get; init; }
    
    /// <summary>
    /// Identificador do cliente.
    /// </summary>
    public Guid ClienteId { get; init; }
    
    /// <summary>
    /// Identificador da filial.
    /// </summary>
    public Guid FilialId { get; init; }
    
    /// <summary>
    /// Valor total da venda calculado no banco de dados.
    /// </summary>
    public decimal ValorTotal { get; init; }
    
    /// <summary>
    /// Status atual da venda (Ativa, Cancelada).
    /// </summary>
    public string Status { get; init; } = string.Empty;
    
    /// <summary>
    /// Quantidade total de itens na venda calculada no banco de dados.
    /// </summary>
    public int QuantidadeItens { get; init; }
}
