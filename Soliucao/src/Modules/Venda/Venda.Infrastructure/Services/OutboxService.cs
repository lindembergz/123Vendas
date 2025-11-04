using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _123Vendas.Shared.Events;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Entities;
using Venda.Infrastructure.Interfaces;

namespace Venda.Infrastructure.Services;

/// <summary>
/// Implementação do serviço Outbox para garantir consistência transacional
/// entre persistência de dados e publicação de eventos.
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly VendaDbContext _context;
    
    public OutboxService(VendaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
    public async Task AdicionarEventoAsync(IDomainEvent evento, CancellationToken ct = default)
    {
        if (evento == null)
            throw new ArgumentNullException(nameof(evento));
        
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = evento.GetType().AssemblyQualifiedName ?? evento.GetType().FullName ?? evento.GetType().Name,
            EventData = JsonSerializer.Serialize(evento, evento.GetType()),
            OccurredAt = evento.OccurredAt,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        
        await _context.OutboxEvents.AddAsync(outboxEvent, ct);
        //Nota: SaveChangesAsync será chamado pelo repositório na mesma transação
    }
    
    public async Task<List<OutboxEvent>> ObterEventosPendentesAsync(int batchSize = 50, CancellationToken ct = default)
    {
        return await _context.OutboxEvents
            .Where(e => e.Status == "Pending" && e.RetryCount < 5)
            .OrderBy(e => e.OccurredAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }
    
    public async Task MarcarComoProcessadoAsync(Guid eventoId, CancellationToken ct = default)
    {
        var evento = await _context.OutboxEvents.FindAsync(new object[] { eventoId }, ct);
        
        if (evento != null)
        {
            evento.Status = "Processed";
            evento.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }
    
    public async Task MarcarComoFalhadoAsync(Guid eventoId, string erro, CancellationToken ct = default)
    {
        var evento = await _context.OutboxEvents.FindAsync(new object[] { eventoId }, ct);
        
        if (evento != null)
        {
            evento.Status = "Failed";
            evento.RetryCount++;
            evento.LastError = erro;
            await _context.SaveChangesAsync(ct);
        }
    }
}
