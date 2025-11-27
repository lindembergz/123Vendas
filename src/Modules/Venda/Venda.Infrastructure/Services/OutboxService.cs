using System.Buffers;
using System.Text;
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
        
        // Aluga buffer do pool para evitar alocações no heap
        var buffer = ArrayPool<byte>.Shared.Rent(4096); // 4KB inicial, suficiente para maioria dos eventos
        try
        {
            using var stream = new MemoryStream(buffer);
            using var writer = new Utf8JsonWriter(stream);
            
            // Serializa diretamente para UTF-8 sem alocações intermediárias
            JsonSerializer.Serialize(writer, evento, evento.GetType());
            await writer.FlushAsync(ct);
            
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = evento.GetType().AssemblyQualifiedName ?? evento.GetType().FullName ?? evento.GetType().Name,
                EventData = Encoding.UTF8.GetString(buffer, 0, (int)stream.Position),
                OccurredAt = evento.OccurredAt,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.OutboxEvents.AddAsync(outboxEvent, ct);
            //Nota: SaveChangesAsync será chamado pelo repositório na mesma transação
        }
        finally
        {
            // Sempre devolve o buffer ao pool para reutilização
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
