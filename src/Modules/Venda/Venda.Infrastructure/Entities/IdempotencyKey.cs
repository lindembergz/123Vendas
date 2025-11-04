namespace Venda.Infrastructure.Entities;

public class IdempotencyKey
{
    public Guid RequestId { get; set; }
    public string CommandType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
