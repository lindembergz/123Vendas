namespace Venda.Application.DTOs;

public record ItemVendaDto(
    Guid ProdutoId,
    int Quantidade,
    decimal ValorUnitario,
    decimal Desconto,
    decimal Total);
