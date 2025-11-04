using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Venda.Infrastructure.Data;

namespace Venda.Integration.Tests.Infrastructure;

/// <summary>
/// Testes para validar a configuração da CustomWebApplicationFactory.
/// </summary>
public class CustomWebApplicationFactoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomWebApplicationFactoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Factory_DeveSerInicializadaCorretamente()
    {
        
        _factory.Should().NotBeNull();
    }

    [Fact]
    public void Factory_DeveConfigurarDbContextComSQLiteInMemory()
    {
         & Act
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        
        dbContext.Should().NotBeNull();
        dbContext.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
    }

    [Fact]
    public void Factory_DeveCriarHttpClient()
    {
        
        var client = _factory.CreateClient();

        
        client.Should().NotBeNull();
        client.BaseAddress.Should().NotBeNull();
    }

    [Fact]
    public async Task Factory_DevePermitirCriacaoDeTabelasNoBanco()
    {
        
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        
        var canConnect = await dbContext.Database.CanConnectAsync();

        
        canConnect.Should().BeTrue();
    }

    [Fact]
    public void Factory_DeveInicializarBancoComTabelas()
    {
        
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        
        var vendas = dbContext.Vendas.ToList();
        var outboxEvents = dbContext.OutboxEvents.ToList();
        var idempotencyKeys = dbContext.IdempotencyKeys.ToList();

         - Tabelas devem existir (mesmo que vazias)
        vendas.Should().NotBeNull();
        outboxEvents.Should().NotBeNull();
        idempotencyKeys.Should().NotBeNull();
    }
}
