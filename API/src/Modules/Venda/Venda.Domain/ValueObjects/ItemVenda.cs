namespace Venda.Domain.ValueObjects;

public record ItemVenda(
    Guid ProdutoId,
    int Quantidade,
    decimal ValorUnitario,
    decimal Desconto = 0m)
{
    public decimal Total => Quantidade * ValorUnitario * (1 - Desconto);
    
    public ItemVenda WithDesconto(decimal desconto) => this with { Desconto = desconto };
    public ItemVenda WithQuantidade(int novaQuantidade) => this with { Quantidade = novaQuantidade };
}
