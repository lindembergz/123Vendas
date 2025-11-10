using Microsoft.EntityFrameworkCore;
using Venda.Domain.Aggregates;
using Venda.Domain.Interfaces;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Entities;
using Venda.Infrastructure.Interfaces;

namespace Venda.Infrastructure.Repositories;

public class VendaRepository : IVendaRepository
{
    private readonly VendaDbContext _context;
    private readonly IOutboxService _outboxService;
    
    public VendaRepository(VendaDbContext context, IOutboxService outboxService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
    }
    
    public async Task<VendaAgregado?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Vendas
            .Include(v => v.Produtos)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }
    
    public async Task<List<VendaAgregado>> ListarAsync(CancellationToken ct = default)
    {
        return await _context.Vendas
            .Include(v => v.Produtos)
            .AsNoTracking()
            .OrderByDescending(v => v.Data)
            .ToListAsync(ct);
    }
    
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
        
        //Aplicar filtros
        if (clienteId.HasValue)
            query = query.Where(v => v.ClienteId == clienteId.Value);
        
        if (filialId.HasValue)
        {
            query = query.Where(v => v.FilialId == filialId.Value);
        }
        
        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);
        
        if (dataInicio.HasValue)
            query = query.Where(v => v.Data >= dataInicio.Value);
        
        if (dataFim.HasValue)
            query = query.Where(v => v.Data <= dataFim.Value);
        
        //Contar total antes da paginação
        var totalCount = await query.CountAsync(ct);
        
        //Aplicar paginação e ordenação
        var items = await query
            .Include(v => v.Produtos)
            .OrderByDescending(v => v.Data)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        return (items, totalCount);
    }
    
    public async Task AdicionarAsync(VendaAgregado venda, CancellationToken ct = default)
    {
        if (venda == null)
            throw new ArgumentNullException(nameof(venda));
        
        const int maxRetries = 3;
        var retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
            {
                // Gerar número sequencial de forma thread-safe
                var novoNumero = await ObterProximoNumeroVendaAsync(venda.FilialId, ct);
                venda.DefinirNumeroVenda(novoNumero);
                
                await _context.Vendas.AddAsync(venda, ct);
                
                // Adicionar eventos ao outbox na mesma transação
                foreach (var evento in venda.DomainEvents)
                {
                    await _outboxService.AdicionarEventoAsync(evento, ct);
                }
                
                await _context.SaveChangesAsync(ct);
                
                // Limpar eventos após persistir
                venda.ClearDomainEvents();
                
                return; // Sucesso
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true 
                                               || ex.InnerException?.Message.Contains("IX_Vendas_FilialId_NumeroVenda_Unique") == true)
            {
                // Violação de constraint única - número duplicado (race condition extremamente rara)
                retryCount++;
                
                if (retryCount >= maxRetries)
                {
                    throw new InvalidOperationException(
                        $"Falha ao criar venda após {maxRetries} tentativas devido a conflito de número sequencial. " +
                        $"FilialId: {venda.FilialId}", ex);
                }
                
                // Limpar o contexto e tentar novamente
                _context.ChangeTracker.Clear();
                venda.ClearDomainEvents(); // Limpar eventos para não duplicar
                
                // Aguardar antes de tentar novamente
                await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount), ct);
            }
        }
    }
    
    public async Task AtualizarAsync(VendaAgregado venda, CancellationToken ct = default)
    {
        if (venda == null)
            throw new ArgumentNullException(nameof(venda));
        
        _context.Vendas.Update(venda);
        
        //Adicionar eventos ao outbox na mesma transação
        foreach (var evento in venda.DomainEvents)
        {
            await _outboxService.AdicionarEventoAsync(evento, ct);
        }
        
        await _context.SaveChangesAsync(ct);
        
        //Limpar eventos após persistir
        venda.ClearDomainEvents();
    }
    
    public async Task<bool> ExisteAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Vendas
            .AsNoTracking()
            .AnyAsync(v => v.Id == id, ct);
    }
    
    public async Task<int> ObterUltimoNumeroPorFilialAsync(Guid filialId, CancellationToken ct = default)
    {
        // OBSOLETO: Este método não deve mais ser usado diretamente.
        // Use ObterProximoNumeroVendaAsync para geração thread-safe.
        var ultimoNumero = await _context.Vendas
            .AsNoTracking()
            .Where(v => v.FilialId == filialId)
            .MaxAsync(v => (int?)v.NumeroVenda, ct);
        
        return ultimoNumero ?? 0;
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
        const int maxRetries = 5;
        var retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
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
            }
            catch (DbUpdateConcurrencyException)
            {
                // Conflito de concorrência - outra thread atualizou a sequência
                retryCount++;
                
                if (retryCount >= maxRetries)
                {
                    throw new InvalidOperationException(
                        $"Falha ao obter próximo número de venda após {maxRetries} tentativas. " +
                        $"FilialId: {filialId}");
                }
                
                // Limpar o contexto e tentar novamente
                _context.ChangeTracker.Clear();
                
                // Aguardar um tempo exponencial antes de tentar novamente
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 10), ct);
            }
        }
        
        throw new InvalidOperationException(
            $"Falha inesperada ao obter próximo número de venda. FilialId: {filialId}");
    }
}
