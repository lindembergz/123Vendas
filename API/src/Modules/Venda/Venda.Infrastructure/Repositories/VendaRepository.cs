using Microsoft.EntityFrameworkCore;
using Venda.Domain.Aggregates;
using Venda.Domain.Interfaces;
using Venda.Infrastructure.Data;
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
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }
    
    public async Task<List<VendaAgregado>> ListarAsync(CancellationToken ct = default)
    {
        return await _context.Vendas
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
        
        // Aplicar filtros
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
        
        // Contar total antes da paginação
        var totalCount = await query.CountAsync(ct);
        
        // Aplicar paginação e ordenação
        var items = await query
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
        
        await _context.Vendas.AddAsync(venda, ct);
        
        // Adicionar eventos ao outbox na mesma transação
        foreach (var evento in venda.DomainEvents)
        {
            await _outboxService.AdicionarEventoAsync(evento, ct);
        }
        
        await _context.SaveChangesAsync(ct);
        
        // Limpar eventos após persistir
        venda.ClearDomainEvents();
    }
    
    public async Task AtualizarAsync(VendaAgregado venda, CancellationToken ct = default)
    {
        if (venda == null)
            throw new ArgumentNullException(nameof(venda));
        
        _context.Vendas.Update(venda);
        
        // Adicionar eventos ao outbox na mesma transação
        foreach (var evento in venda.DomainEvents)
        {
            await _outboxService.AdicionarEventoAsync(evento, ct);
        }
        
        await _context.SaveChangesAsync(ct);
        
        // Limpar eventos após persistir
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
        var ultimoNumero = await _context.Vendas
            .AsNoTracking()
            .Where(v => v.FilialId == filialId)
            .MaxAsync(v => (int?)v.NumeroVenda, ct);
        
        return ultimoNumero ?? 0;
    }
}
