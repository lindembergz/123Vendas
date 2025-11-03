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
        // Assert
        _factory.Should().NotBeNull();
    }

    [Fact]
    public void Factory_DeveConfigurarDbContextComSQLiteInMemory()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
        dbContext.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
    }

    [Fact]
    public void Factory_DeveCriarHttpClient()
    {
        // Act
        var client = _factory.CreateClient();

        // Assert
        client.Should().NotBeNull();
        client.BaseAddress.Should().NotBeNull();
    }

    [Fact]
    public async Task Factory_DevePermitirCriacaoDeTabelasNoBanco()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        // Act
        var canConnect = await dbContext.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public void Factory_DeveInicializarBancoComTabelas()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        // Act
        var vendas = dbContext.Vendas.ToList();
        var outboxEvents = dbContext.OutboxEvents.ToList();
        var idempotencyKeys = dbContext.IdempotencyKeys.ToList();

        // Assert - Tabelas devem existir (mesmo que vazias)
        vendas.Should().NotBeNull();
        outboxEvents.Should().NotBeNull();
        idempotencyKeys.Should().NotBeNull();
    }
}
