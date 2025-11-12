using _123Vendas.Shared.Common;
using Venda.Domain.Aggregates;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Specifications;

/// <summary>
/// Interface para especificações de validação de itens de venda.
/// Implementa o Specification Pattern para encapsular regras de negócio reutilizáveis.
/// </summary>
public interface IItemVendaSpecification
{
    /// <summary>
    /// Verifica se o item satisfaz a especificação no contexto da venda.
    /// </summary>
    /// <param name="item">Item a ser validado</param>
    /// <param name="venda">Venda que contém o item</param>
    /// <returns>Result.Success se satisfaz, Result.Failure com mensagem de erro caso contrário</returns>
    Result IsSatisfiedBy(ItemVenda item, VendaAgregado venda);
}
