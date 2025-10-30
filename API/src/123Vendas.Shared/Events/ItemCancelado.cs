namespace _123Vendas.Shared.Events;

/// <summary>
/// Evento publicado quando um item específico é removido de uma venda.
/// Consumido pelo módulo de Estoque para ajuste de estoque do produto removido.
/// </summary>
public record ItemCancelado : DomainEvent
{
    public Guid VendaId { get; init; }
    public Guid ProdutoId { get; init; }

    public ItemCancelado(Guid vendaId, Guid produtoId)
    {
        VendaId = vendaId;
        ProdutoId = produtoId;
    }

    // Construtor para desserialização
    private ItemCancelado() { }
}
