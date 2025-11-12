using _123Vendas.Shared.Common;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Specifications;

/// <summary>
/// Especificação que valida se a venda está ativa (não cancelada).
/// </summary>
public class VendaAtivaSpecification : IItemVendaSpecification
{
    /// <summary>
    /// Verifica se a venda não está cancelada.
    /// </summary>
    public Result IsSatisfiedBy(ItemVenda item, VendaAgregado venda)
    {
        if (venda.Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível adicionar itens a uma venda cancelada.");
        
        return Result.Success();
    }
}
