using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venda.Infrastructure.Entities;

namespace Venda.Infrastructure.Configurations;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("IdempotencyKeys");
        
        // Primary Key
        builder.HasKey(k => k.RequestId);
        
        // Properties
        builder.Property(k => k.RequestId)
            .IsRequired();
        
        builder.Property(k => k.CommandType)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(k => k.AggregateId)
            .IsRequired();
        
        builder.Property(k => k.CreatedAt)
            .IsRequired();
        
        builder.Property(k => k.ExpiresAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(k => k.ExpiresAt)
            .HasDatabaseName("IX_IdempotencyKeys_ExpiresAt");
    }
}
