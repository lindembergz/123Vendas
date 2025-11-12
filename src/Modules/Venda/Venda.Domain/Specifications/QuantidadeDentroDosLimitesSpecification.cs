using _123Vendas.Shared.Common;
using Venda.Domain.Aggregates;
using Venda.Domain.Interfaces;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Specifications;

/// <summary>
/// Especificação que valida se a quantidade do item está dentro dos limites permitidos.
/// Usa IPoliticaDesconto para verificar se a quantidade total (existente + nova) é permitida.
/// </summary>
public class QuantidadeDentroDosLimitesSpecification : IItemVendaSpecification
{
    private readonly IPoliticaDesconto _politicaDesconto;
    
    /// <summary>
    /// Inicializa a especificação com a política de desconto.
    /// </summary>
    /// <param name="politicaDesconto">Política de desconto que define os limites</param>
    public QuantidadeDentroDosLimitesSpecification(IPoliticaDesconto politicaDesconto)
    {
        _politicaDesconto = politicaDesconto ?? throw new ArgumentNullException(nameof(politicaDesconto));
    }
    
    /// <summary>
    /// Verifica se a quantidade total (existente + nova) está dentro dos limites permitidos.
    /// </summary>
    public Result IsSatisfiedBy(ItemVenda item, VendaAgregado venda)
    {
        var quantidadeExistente = venda.Produtos
            .Where(i => i.ProdutoId == item.ProdutoId)
            .Sum(i => i.Quantidade);
        
        var quantidadeTotal = quantidadeExistente + item.Quantidade;
        
        if (!_politicaDesconto.PermiteVenda(quantidadeTotal))
            return Result.Failure("Não é permitido vender mais de 20 unidades do mesmo produto.");
        
        return Result.Success();
    }
}
