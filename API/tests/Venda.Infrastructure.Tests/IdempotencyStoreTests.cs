using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Services;

namespace Venda.Infrastructure.Tests;

public class IdempotencyStoreTests : IDisposable
{
    private readonly VendaDbContext _context;
    private readonly IdempotencyStore _idempotencyStore;
    
    public IdempotencyStoreTests()
    {
        // Configurar banco em memória
        var options = new DbContextOptionsBuilder<VendaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new VendaDbContext(options);
        _idempotencyStore = new IdempotencyStore(_context);
    }
    
    [Fact]
    public async Task ExistsAsync_DeveRetornarTrueParaRequestIdExistente()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var commandType = "CriarVendaCommand";
        var aggregateId = Guid.NewGuid();
        
        await _idempotencyStore.SaveAsync(requestId, commandType, aggregateId);
        
        // Act
        var exists = await _idempotencyStore.ExistsAsync(requestId);
        
        // Assert
        exists.Should().BeTrue();
    }
    
    [Fact]
    public async Task ExistsAsync_DeveRetornarFalseParaRequestIdInexistente()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        
        // Act
        var exists = await _idempotencyStore.ExistsAsync(requestId);
        
        // Assert
        exists.Should().BeFalse();
    }
    
    [Fact]
    public async Task ExistsAsync_DeveRetornarFalseParaRequestIdExpirado()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var commandType = "CriarVendaCommand";
        var aggregateId = Guid.NewGuid();
        
        await _idempotencyStore.SaveAsync(requestId, commandType, aggregateId);
        
        // Forçar expiração
        var key = await _context.IdempotencyKeys.FirstAsync(k => k.RequestId == requestId);
        key.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();
        
        // Act
        var exists = await _idempotencyStore.ExistsAsync(requestId);
        
        // Assert
        exists.Should().BeFalse();
    }
    
    [Fact]
    public async Task SaveAsync_DeveSalvarChaveCorretamente()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var commandType = "CriarVendaCommand";
        var aggregateId = Guid.NewGuid();
        
        // Act
        await _idempotencyStore.SaveAsync(requestId, commandType, aggregateId);
        
        // Assert
        var key = await _context.IdempotencyKeys.FirstOrDefaultAsync(k => k.RequestId == requestId);
        key.Should().NotBeNull();
        key!.RequestId.Should().Be(requestId);
        key.CommandType.Should().Be(commandType);
        key.AggregateId.Should().Be(aggregateId);
        key.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        key.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public async Task SaveAsync_DeveConfigurarExpiracaoDe7Dias()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var commandType = "CriarVendaCommand";
        var aggregateId = Guid.NewGuid();
        
        // Act
        await _idempotencyStore.SaveAsync(requestId, commandType, aggregateId);
        
        // Assert
        var key = await _context.IdempotencyKeys.FirstAsync(k => k.RequestId == requestId);
        var expectedExpiration = DateTime.UtcNow.AddDays(7);
        key.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public async Task GetAggregateIdAsync_DeveRetornarIdCorreto()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var commandType = "CriarVendaCommand";
        var aggregateId = Guid.NewGuid();
        
        await _idempotencyStore.SaveAsync(requestId, commandType, aggregateId);
        
        // Act
        var retrievedAggregateId = await _idempotencyStore.GetAggregateIdAsync(requestId);
        
        // Assert
        retrievedAggregateId.Should().NotBeNull();
        retrievedAggregateId.Should().Be(aggregateId);
    }
    
    [Fact]
    public async Task GetAggregateIdAsync_DeveRetornarNullParaRequestIdInexistente()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        
        // Act
        var retrievedAggregateId = await _idempotencyStore.GetAggregateIdAsync(requestId);
        
        // Assert
        retrievedAggregateId.Should().BeNull();
    }
    
    [Fact]
    public async Task GetAggregateIdAsync_DeveRetornarNullParaRequestIdExpirado()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var commandType = "CriarVendaCommand";
        var aggregateId = Guid.NewGuid();
        
        await _idempotencyStore.SaveAsync(requestId, commandType, aggregateId);
        
        // Forçar expiração
        var key = await _context.IdempotencyKeys.FirstAsync(k => k.RequestId == requestId);
        key.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();
        
        // Act
        var retrievedAggregateId = await _idempotencyStore.GetAggregateIdAsync(requestId);
        
        // Assert
        retrievedAggregateId.Should().BeNull();
    }
    
    [Fact]
    public async Task SaveAsync_DevePermitirMultiplasChavesComDiferentesRequestIds()
    {
        // Arrange
        var requestId1 = Guid.NewGuid();
        var requestId2 = Guid.NewGuid();
        var commandType = "CriarVendaCommand";
        var aggregateId1 = Guid.NewGuid();
        var aggregateId2 = Guid.NewGuid();
        
        // Act
        await _idempotencyStore.SaveAsync(requestId1, commandType, aggregateId1);
        await _idempotencyStore.SaveAsync(requestId2, commandType, aggregateId2);
        
        // Assert
        var keys = await _context.IdempotencyKeys.ToListAsync();
        keys.Should().HaveCount(2);
        
        var key1 = keys.First(k => k.RequestId == requestId1);
        var key2 = keys.First(k => k.RequestId == requestId2);
        
        key1.AggregateId.Should().Be(aggregateId1);
        key2.AggregateId.Should().Be(aggregateId2);
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}
