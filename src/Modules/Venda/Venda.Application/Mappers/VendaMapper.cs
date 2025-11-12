using Venda.Application.DTOs;
using Venda.Domain.Aggregates;

namespace Venda.Application.Mappers;

/// <summary>
/// Centralized mapper for converting VendaAgregado to VendaDto.
/// Eliminates code duplication across handlers.
/// </summary>
public static class VendaMapper
{
    /// <summary>
    /// Maps a single VendaAgregado to VendaDto.
    /// </summary>
    /// <param name="venda">The venda aggregate to map.</param>
    /// <returns>The mapped VendaDto.</returns>
    /// <exception cref="ArgumentNullException">Thrown when venda is null.</exception>
    public static VendaDto ToDto(this VendaAgregado venda)
    {
        if (venda == null)
            throw new ArgumentNullException(nameof(venda));

        var itensDto = venda.Produtos
            .Select(item => new ItemVendaDto(
                item.ProdutoId,
                item.Quantidade,
                item.ValorUnitario,
                item.Desconto,
                item.Total))
            .ToList();

        return new VendaDto(
            venda.Id,
            venda.NumeroVenda,
            venda.Data,
            venda.ClienteId,
            venda.FilialId,
            venda.ValorTotal,
            venda.Status.ToString(),
            itensDto);
    }

    /// <summary>
    /// Maps a collection of VendaAgregado to a list of VendaDto.
    /// </summary>
    /// <param name="vendas">The collection of venda aggregates to map.</param>
    /// <returns>A list of mapped VendaDtos.</returns>
    /// <exception cref="ArgumentNullException">Thrown when vendas is null.</exception>
    public static List<VendaDto> ToDto(this IEnumerable<VendaAgregado> vendas)
    {
        if (vendas == null)
            throw new ArgumentNullException(nameof(vendas));

        return vendas.Select(ToDto).ToList();
    }
}
