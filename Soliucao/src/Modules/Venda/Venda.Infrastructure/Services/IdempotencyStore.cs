using Microsoft.EntityFrameworkCore;
using Venda.Application.Interfaces;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Entities;

namespace Venda.Infrastructure.Services;

public class IdempotencyStore : IIdempotencyStore
{
    private readonly VendaDbContext _context;
    private const int ExpirationDays = 7;

    public IdempotencyStore(VendaDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid requestId, CancellationToken ct = default)
    {
        return await _context.IdempotencyKeys
            .AnyAsync(k => k.RequestId == requestId && k.ExpiresAt > DateTime.UtcNow, ct);
    }

    public async Task SaveAsync(Guid requestId, string commandType, Guid aggregateId, CancellationToken ct = default)
    {
        var key = new IdempotencyKey
        {
            RequestId = requestId,
            CommandType = commandType,
            AggregateId = aggregateId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(ExpirationDays)
        };

        _context.IdempotencyKeys.Add(key);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Guid?> GetAggregateIdAsync(Guid requestId, CancellationToken ct = default)
    {
        var key = await _context.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.RequestId == requestId && k.ExpiresAt > DateTime.UtcNow, ct);

        return key?.AggregateId;
    }
}
