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
        
        // Configuração para migrations (usa SQLite na pasta da API)
        // Caminho relativo a partir da pasta Venda.Infrastructure até a API
        var dbPath = Path.Combine("..", "..", "..", "123Vendas.Api", "vendas.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}", sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(30);
        });
        
        return new VendaDbContext(optionsBuilder.Options);
    }
}
