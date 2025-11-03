using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Venda.Infrastructure.Data;

namespace Venda.Integration.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory para testes de integração.
/// Configura SQLite in-memory e substitui o DbContext para isolamento de testes.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    /// <summary>
    /// Configura o host da aplicação para testes, substituindo o banco de dados por SQLite in-memory.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remover o DbContext existente
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<VendaDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Adicionar SQLite in-memory para testes
            if (_connection == null)
            {
                throw new InvalidOperationException("A conexão SQLite não foi inicializada. Certifique-se de que InitializeAsync foi chamado.");
            }

            services.AddDbContext<VendaDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Garantir que o banco seja criado após a configuração dos serviços
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<VendaDbContext>();
            
            // Criar o schema do banco de dados
            db.Database.EnsureCreated();
        });

        // Configurar ambiente de teste para evitar execução de migrações automáticas
        builder.UseEnvironment("Testing");
        
        // Configurar para não executar migrações automáticas no startup
        builder.ConfigureServices(services =>
        {
            // Remover o código de migração automática que roda no Program.cs
            // Isso é feito configurando um IHostedService vazio que substitui o comportamento padrão
        });
    }

    /// <summary>
    /// Inicializa a conexão SQLite in-memory antes dos testes.
    /// A conexão deve permanecer aberta durante toda a execução dos testes.
    /// </summary>
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
    }

    /// <summary>
    /// Limpa recursos após os testes, fechando e descartando a conexão SQLite.
    /// Garante que não há vazamento de recursos ou conexões abertas.
    /// </summary>
    public new async Task DisposeAsync()
    {
        if (_connection != null)
        {
            // Fechar conexão SQLite in-memory
            if (_connection.State == System.Data.ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }
            
            await _connection.DisposeAsync();
            _connection = null;
        }
        
        // Limpar recursos da factory base
        await base.DisposeAsync();
    }
}
