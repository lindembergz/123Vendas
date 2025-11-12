using Microsoft.EntityFrameworkCore;
using Venda.Application.DTOs;
using Venda.Application.Interfaces;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Infrastructure.Data;

namespace Venda.Infrastructure.Repositories;

/// <summary>
/// Repositório otimizado para queries de vendas com projection direta para DTOs.
/// Implementa padrão CQRS separando queries de comandos para melhor performance.
/// </summary>
public class VendaQueryRepository : IVendaQueryRepository
{
    private readonly VendaDbContext _context;
    
    /// <summary>
    /// Inicializa uma nova instância de <see cref="VendaQueryRepository"/>.
    /// </summary>
    /// <param name="context">Contexto do banco de dados</param>
    /// <exception cref="ArgumentNullException">Quando context é nulo</exception>
    public VendaQueryRepository(VendaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
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
    public async Task<(List<VendaListDto> Items, int TotalCount)> ListarComProjecaoAsync(
        int pageNumber,
        int pageSize,
        Guid? clienteId = null,
        Guid? filialId = null,
        StatusVenda? status = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        CancellationToken ct = default)
    {
        var query = _context.Vendas.AsNoTracking();
        
        // Aplicar filtros usando método extraído
        query = AplicarFiltros(query, clienteId, filialId, status, dataInicio, dataFim);
        
        // Projection direta para DTO no banco de dados
        // ValorTotal e QuantidadeItens são calculados no SQL
        var items = await query
            .OrderByDescending(v => v.Data)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VendaListDto
            {
                Id = v.Id,
                NumeroVenda = v.NumeroVenda,
                Data = v.Data,
                ClienteId = v.ClienteId,
                FilialId = v.FilialId,
                // Calcula Total no SQL: Quantidade * ValorUnitario * (1 - Desconto)
                ValorTotal = v.Produtos.Sum(p => p.Quantidade * p.ValorUnitario * (1 - p.Desconto)),
                Status = v.Status.ToString(),
                QuantidadeItens = v.Produtos.Count
            })
            .ToListAsync(ct);
        
        // Otimização: se primeira página retornou menos itens que pageSize, não precisa fazer count
        int totalCount;
        if (pageNumber == 1 && items.Count < pageSize)
        {
            totalCount = items.Count;
        }
        else
        {
            totalCount = await query.CountAsync(ct);
        }
        
        return (items, totalCount);
    }
    
    /// <summary>
    /// Aplica filtros à query de vendas de forma centralizada.
    /// Extrai lógica de filtragem para reduzir duplicação e melhorar manutenibilidade.
    /// </summary>
    /// <param name="query">Query base de vendas</param>
    /// <param name="clienteId">Filtro opcional por cliente</param>
    /// <param name="filialId">Filtro opcional por filial</param>
    /// <param name="status">Filtro opcional por status</param>
    /// <param name="dataInicio">Filtro opcional por data inicial</param>
    /// <param name="dataFim">Filtro opcional por data final</param>
    /// <returns>Query com filtros aplicados</returns>
    private static IQueryable<VendaAgregado> AplicarFiltros(
        IQueryable<VendaAgregado> query,
        Guid? clienteId,
        Guid? filialId,
        StatusVenda? status,
        DateTime? dataInicio,
        DateTime? dataFim)
    {
        if (clienteId.HasValue)
            query = query.Where(v => v.ClienteId == clienteId.Value);
        
        if (filialId.HasValue)
            query = query.Where(v => v.FilialId == filialId.Value);
        
        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);
        
        if (dataInicio.HasValue)
            query = query.Where(v => v.Data >= dataInicio.Value);
        
        if (dataFim.HasValue)
            query = query.Where(v => v.Data <= dataFim.Value);
        
        return query;
    }
}
