namespace Venda.Infrastructure.Interfaces;

public interface IIdempotencyStore
{
    Task<bool> ExistsAsync(Guid requestId, CancellationToken ct = default);
    Task SaveAsync(Guid requestId, string commandType, Guid aggregateId, CancellationToken ct = default);
    Task<Guid?> GetAggregateIdAsync(Guid requestId, CancellationToken ct = default);
}
