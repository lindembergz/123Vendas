using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venda.Infrastructure.Entities;

namespace Venda.Infrastructure.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(e => e.EventData)
            .IsRequired();
        
        builder.Property(e => e.OccurredAt)
            .IsRequired();
        
        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Pending");
        
        builder.Property(e => e.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);
        
        builder.Property(e => e.CreatedAt)
            .IsRequired();
        
        // Índices para performance
        builder.HasIndex(e => new { e.Status, e.OccurredAt })
            .HasDatabaseName("IX_OutboxEvents_Status_OccurredAt");
    }
}
