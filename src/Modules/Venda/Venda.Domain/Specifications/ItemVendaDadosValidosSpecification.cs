using _123Vendas.Shared.Common;
using Venda.Domain.Aggregates;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Specifications;

/// <summary>
/// Especificação que valida se os dados básicos do item de venda são válidos.
/// Verifica ProdutoId, Quantidade e ValorUnitario.
/// </summary>
public class ItemVendaDadosValidosSpecification : IItemVendaSpecification
{
    private const decimal MAX_VALOR_UNITARIO = 999999.99m;
    
    /// <summary>
    /// Verifica se o item possui dados válidos (ProdutoId, Quantidade > 0, ValorUnitario válido).
    /// </summary>
    public Result IsSatisfiedBy(ItemVenda item, VendaAgregado venda)
    {
        if (item.ProdutoId == Guid.Empty)
            return Result.Failure("ProdutoId é obrigatório.");
        
        if (item.Quantidade <= 0)
            return Result.Failure("Quantidade deve ser maior que zero.");
        
        if (item.ValorUnitario <= 0 || item.ValorUnitario > MAX_VALOR_UNITARIO)
            return Result.Failure($"Valor unitário deve ser maior que zero e menor que {MAX_VALOR_UNITARIO}.");
        
        return Result.Success();
    }
}
