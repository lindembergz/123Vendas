using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Venda.Infrastructure.Data;

/// <summary>
/// Factory para criação do DbContext em tempo de design (migrations)
/// </summary>
public class VendaDbContextFactory : IDesignTimeDbContextFactory<VendaDbContext>
{
    public VendaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VendaDbContext>();
        
        // Configuração para migrations (usa SQLite local)
        optionsBuilder.UseSqlite("Data Source=vendas.db", sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(30);
        });
        
        return new VendaDbContext(optionsBuilder.Options);
    }
}
