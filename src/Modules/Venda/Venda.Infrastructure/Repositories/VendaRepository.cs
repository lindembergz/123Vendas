using Microsoft.EntityFrameworkCore;
using Venda.Domain.Aggregates;
using Venda.Domain.Interfaces;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Entities;
using Venda.Infrastructure.Interfaces;

namespace Venda.Infrastructure.Repositories;

/// <summary>
/// Repositório para operações de persistência do agregado VendaAgregado.
/// Implementa padrão Repository com suporte a retry strategy e outbox pattern.
/// </summary>
public class VendaRepository : IVendaRepository
{
    private readonly VendaDbContext _context;
    private readonly IOutboxService _outboxService;
    private readonly IRetryStrategy _retryStrategy;
    
    /// <summary>
    /// Inicializa uma nova instância de <see cref="VendaRepository"/>.
    /// </summary>
    /// <param name="context">Contexto do banco de dados</param>
    /// <param name="outboxService">Serviço para gerenciar eventos do outbox</param>
    /// <param name="retryStrategy">Estratégia de retry para operações com conflito de concorrência</param>
    /// <exception cref="ArgumentNullException">Quando algum parâmetro é nulo</exception>
    public VendaRepository(
        VendaDbContext context, 
        IOutboxService outboxService,
        IRetryStrategy retryStrategy)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
        _retryStrategy = retryStrategy ?? throw new ArgumentNullException(nameof(retryStrategy));
    }
    
    /// <summary>
    /// Obtém uma venda por ID incluindo seus itens.
    /// </summary>
    /// <param name="id">ID da venda</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Venda encontrada ou null se não existir</returns>
    public async Task<VendaAgregado?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Vendas
            .Include(v => v.Produtos)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }
    
    /// <summary>
    /// Lista todas as vendas ordenadas por data decrescente.
    /// </summary>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Lista de vendas</returns>
    public async Task<List<VendaAgregado>> ListarAsync(CancellationToken ct = default)
    {
        return await _context.Vendas
            .Include(v => v.Produtos)
            .AsNoTracking()
            .OrderByDescending(v => v.Data)
            .ToListAsync(ct);
    }
    
    /// <summary>
    /// Lista vendas com filtros e paginação.
    /// Otimizado com AsSplitQuery para evitar cartesian explosion.
    /// Otimiza count query quando possível (primeira página com menos itens que pageSize).
    /// </summary>
    /// <param name="pageNumber">Número da página (base 1)</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="clienteId">Filtro opcional por cliente</param>
    /// <param name="filialId">Filtro opcional por filial</param>
    /// <param name="status">Filtro opcional por status</param>
    /// <param name="dataInicio">Filtro opcional por data inicial</param>
    /// <param name="dataFim">Filtro opcional por data final</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Tupla contendo lista de vendas e total de registros</returns>
    public async Task<(List<VendaAgregado> Items, int TotalCount)> ListarComFiltrosAsync(
        int pageNumber,
        int pageSize,
        Guid? clienteId = null,
        Guid? filialId = null,
        Domain.Enums.StatusVenda? status = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        CancellationToken ct = default)
    {
        var query = _context.Vendas.AsNoTracking();
        
        // Aplicar filtros usando método extraído
        query = AplicarFiltros(query, clienteId, filialId, status, dataInicio, dataFim);
        
        // Aplicar paginação e ordenação com AsSingleQuery (otimizado para paginação)
        // Com paginação, JOIN único + TOP/OFFSET é mais eficiente que split queries
        var items = await query
            .Include(v => v.Produtos)
            .OrderByDescending(v => v.Data)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSingleQuery() 
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
    /// Adiciona uma nova venda ao repositório com retry automático em caso de conflito.
    /// Gera número sequencial thread-safe e persiste eventos de domínio no outbox.
    /// </summary>
    /// <param name="venda">Venda a ser adicionada</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <exception cref="ArgumentNullException">Quando venda é nula</exception>
    /// <exception cref="InvalidOperationException">Quando falha após múltiplas tentativas</exception>
    public async Task AdicionarAsync(VendaAgregado venda, CancellationToken ct = default)
    {
        if (venda == null)
            throw new ArgumentNullException(nameof(venda));
        
        await _retryStrategy.ExecuteAsync(async () =>
        {
            await PersistirVendaComEventosAsync(venda, ct);
            return true;
        }, ct);
    }
    
    /// <summary>
    /// Atualiza uma venda existente e persiste eventos de domínio no outbox.
    /// Utiliza retry strategy para lidar com conflitos de concorrência otimista.
    /// </summary>
    /// <param name="venda">Venda a ser atualizada</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <exception cref="ArgumentNullException">Quando venda é nula</exception>
    /// <exception cref="InvalidOperationException">Quando falha após múltiplas tentativas</exception>
    public async Task AtualizarAsync(VendaAgregado venda, CancellationToken ct = default)
    {
        if (venda == null)
            throw new ArgumentNullException(nameof(venda));
        
        await _retryStrategy.ExecuteAsync(async () =>
        {
            _context.Vendas.Update(venda);
            
            await AdicionarEventosAoOutboxAsync(venda, ct);
            
            await _context.SaveChangesAsync(ct);
            
            venda.ClearDomainEvents();
            
            return true;
        }, ct);
    }
    
    /// <summary>
    /// Verifica se uma venda existe pelo ID.
    /// </summary>
    /// <param name="id">ID da venda</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>True se a venda existe, false caso contrário</returns>
    public async Task<bool> ExisteAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Vendas
            .AsNoTracking()
            .AnyAsync(v => v.Id == id, ct);
    }
    
    /// <summary>
    /// Obtém o último número de venda por filial.
    /// OBSOLETO: Este método não deve mais ser usado diretamente.
    /// Use ObterProximoNumeroVendaAsync para geração thread-safe.
    /// </summary>
    /// <param name="filialId">ID da filial</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Último número de venda ou 0 se não houver vendas</returns>
    public async Task<int> ObterUltimoNumeroPorFilialAsync(Guid filialId, CancellationToken ct = default)
    {
        var ultimoNumero = await _context.Vendas
            .AsNoTracking()
            .Where(v => v.FilialId == filialId)
            .MaxAsync(v => (int?)v.NumeroVenda, ct);
        
        return ultimoNumero ?? 0;
    }
    
    /// <summary>
    /// Persiste a venda com seus eventos de domínio no outbox.
    /// Gera número sequencial thread-safe antes de persistir.
    /// </summary>
    /// <param name="venda">Venda a ser persistida</param>
    /// <param name="ct">Token de cancelamento</param>
    private async Task PersistirVendaComEventosAsync(VendaAgregado venda, CancellationToken ct)
    {
        // Gerar número sequencial de forma thread-safe
        var novoNumero = await ObterProximoNumeroVendaAsync(venda.FilialId, ct);
        venda.DefinirNumeroVenda(novoNumero);
        
        await _context.Vendas.AddAsync(venda, ct);
        
        await AdicionarEventosAoOutboxAsync(venda, ct);
        
        await _context.SaveChangesAsync(ct);
        
        venda.ClearDomainEvents();
    }
    
    /// <summary>
    /// Adiciona os eventos de domínio da venda ao outbox para processamento assíncrono.
    /// </summary>
    /// <param name="venda">Venda contendo os eventos de domínio</param>
    /// <param name="ct">Token de cancelamento</param>
    private async Task AdicionarEventosAoOutboxAsync(VendaAgregado venda, CancellationToken ct)
    {
        foreach (var evento in venda.DomainEvents)
        {
            await _outboxService.AdicionarEventoAsync(evento, ct);
        }
    }
    
    /// <summary>
    /// Obtém o próximo número de venda de forma atômica e thread-safe usando controle de concorrência otimista.
    /// Previne condições de corrida em ambientes com múltiplas requisições concorrentes.
    /// </summary>
    /// <param name="filialId">ID da filial</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Próximo número sequencial disponível</returns>
    /// <exception cref="InvalidOperationException">Quando há falha após múltiplas tentativas de retry</exception>
    private async Task<int> ObterProximoNumeroVendaAsync(Guid filialId, CancellationToken ct = default)
    {
        return await _retryStrategy.ExecuteAsync(async () =>
        {
            // Buscar ou criar a sequência para a filial
            var sequence = await _context.NumeroVendaSequences
                .FirstOrDefaultAsync(s => s.FilialId == filialId, ct);
            
            if (sequence == null)
            {
                // Primeira venda da filial - criar sequência
                sequence = new NumeroVendaSequence
                {
                    FilialId = filialId,
                    UltimoNumero = 0,
                    Versao = 0
                };
                await _context.NumeroVendaSequences.AddAsync(sequence, ct);
            }
            
            // Incrementar o número e a versão
            sequence.UltimoNumero++;
            sequence.Versao++;
            var proximoNumero = sequence.UltimoNumero;
            
            // Salvar com controle de concorrência otimista
            // Se houver conflito, DbUpdateConcurrencyException será lançada
            await _context.SaveChangesAsync(ct);
            
            return proximoNumero;
        }, ct);
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
        Domain.Enums.StatusVenda? status,
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
