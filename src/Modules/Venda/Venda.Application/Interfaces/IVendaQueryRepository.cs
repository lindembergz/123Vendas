using Venda.Application.DTOs;
using Venda.Domain.Enums;

namespace Venda.Application.Interfaces;

/// <summary>
/// Interface para queries otimizadas de vendas com projection direta para DTOs.
/// Separada do IVendaRepository para manter a separação entre comandos e queries (CQRS).
/// </summary>
public interface IVendaQueryRepository
{
    /// <summary>
    /// Lista vendas com filtros e paginação usando projection otimizada.
    /// Projeta diretamente para VendaListDto no banco de dados, evitando carregar agregados completos.
    /// Calcula ValorTotal e QuantidadeItens no SQL para melhor performance.
    /// </summary>
    /// <param name="pageNumber">Número da página (base 1)</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="clienteId">Filtro opcional por cliente</param>
    /// <param name="filialId">Filtro opcional por filial</param>
    /// <param name="status">Filtro opcional por status</param>
    /// <param name="dataInicio">Filtro opcional por data inicial</param>
    /// <param name="dataFim">Filtro opcional por data final</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Tupla contendo lista de DTOs otimizados e total de registros</returns>
    Task<(List<VendaListDto> Items, int TotalCount)> ListarComProjecaoAsync(
        int pageNumber,
        int pageSize,
        Guid? clienteId = null,
        Guid? filialId = null,
        StatusVenda? status = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        CancellationToken ct = default);
}
