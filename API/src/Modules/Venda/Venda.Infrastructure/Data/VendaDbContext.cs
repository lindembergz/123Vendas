using Microsoft.EntityFrameworkCore;
using Venda.Domain.Aggregates;
using Venda.Infrastructure.Entities;

namespace Venda.Infrastructure.Data;

public class VendaDbContext : DbContext
{
    public VendaDbContext(DbContextOptions<VendaDbContext> options) : base(options)
    {
    }
    
    public DbSet<VendaAgregado> Vendas => Set<VendaAgregado>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Aplicar configurações do assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VendaDbContext).Assembly);
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        if (!optionsBuilder.IsConfigured)
        {
            // Configuração padrão para desenvolvimento com timeout de 30 segundos
            optionsBuilder.UseSqlite("Data Source=vendas.db", sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });
        }
    }
}
