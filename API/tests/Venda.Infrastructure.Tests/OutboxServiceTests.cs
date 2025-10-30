using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using _123Vendas.Shared.Events;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Services;

namespace Venda.Infrastructure.Tests;

public class OutboxServiceTests : IDisposable
{
    private readonly VendaDbContext _context;
    private readonly OutboxService _outboxService;
    
    public OutboxServiceTests()
    {
        // Configurar banco em memória
        var options = new DbContextOptionsBuilder<VendaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new VendaDbContext(options);
        _outboxService = new OutboxService(_context);
    }
    
    [Fact]
    public async Task AdicionarEventoAsync_DeveSalvarEventoComStatusPending()
    {
        // Arrange
        var evento = new CompraCriada(Guid.NewGuid(), 1, Guid.NewGuid());
        
        // Act
        await _outboxService.AdicionarEventoAsync(evento);
        await _context.SaveChangesAsync(); // Simula SaveChanges do repositório
        
        // Assert
        var eventosSalvos = await _context.OutboxEvents.ToListAsync();
        eventosSalvos.Should().HaveCount(1);
        
        var eventoSalvo = eventosSalvos[0];
        eventoSalvo.EventType.Should().Contain("CompraCriada");
        eventoSalvo.Status.Should().Be("Pending");
        eventoSalvo.RetryCount.Should().Be(0);
        eventoSalvo.EventData.Should().NotBeNullOrEmpty();
        eventoSalvo.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public async Task ObterEventosPendentesAsync_DeveRetornarApenasEventosPending()
    {
        // Arrange
        var evento1 = new CompraCriada(Guid.NewGuid(), 1, Guid.NewGuid());
        var evento2 = new CompraCriada(Guid.NewGuid(), 2, Guid.NewGuid());
        var evento3 = new CompraCriada(Guid.NewGuid(), 3, Guid.NewGuid());
        
        await _outboxService.AdicionarEventoAsync(evento1);
        await _outboxService.AdicionarEventoAsync(evento2);
        await _outboxService.AdicionarEventoAsync(evento3);
        await _context.SaveChangesAsync();
        
        // Marcar um evento como processado
        var eventos = await _context.OutboxEvents.ToListAsync();
        eventos[1].Status = "Processed";
        await _context.SaveChangesAsync();
        
        // Act
        var eventosPendentes = await _outboxService.ObterEventosPendentesAsync();
        
        // Assert
        eventosPendentes.Should().HaveCount(2);
        eventosPendentes.Should().OnlyContain(e => e.Status == "Pending");
    }
    
    [Fact]
    public async Task ObterEventosPendentesAsync_NaoDeveRetornarEventosComRetryCountMaiorOuIgualA5()
    {
        // Arrange
        var evento1 = new CompraCriada(Guid.NewGuid(), 1, Guid.NewGuid());
        var evento2 = new CompraCriada(Guid.NewGuid(), 2, Guid.NewGuid());
        
        await _outboxService.AdicionarEventoAsync(evento1);
        await _outboxService.AdicionarEventoAsync(evento2);
        await _context.SaveChangesAsync();
        
        // Marcar um evento com 5 retries
        var eventos = await _context.OutboxEvents.ToListAsync();
        eventos[0].RetryCount = 5;
        await _context.SaveChangesAsync();
        
        // Act
        var eventosPendentes = await _outboxService.ObterEventosPendentesAsync();
        
        // Assert
        eventosPendentes.Should().HaveCount(1);
        eventosPendentes[0].RetryCount.Should().BeLessThan(5);
    }
    
    [Fact]
    public async Task ObterEventosPendentesAsync_DeveRespeitarBatchSize()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var evento = new CompraCriada(Guid.NewGuid(), i + 1, Guid.NewGuid());
            await _outboxService.AdicionarEventoAsync(evento);
        }
        await _context.SaveChangesAsync();
        
        // Act
        var eventosPendentes = await _outboxService.ObterEventosPendentesAsync(batchSize: 5);
        
        // Assert
        eventosPendentes.Should().HaveCount(5);
    }
    
    [Fact]
    public async Task MarcarComoProcessadoAsync_DeveAtualizarStatusEProcessedAt()
    {
        // Arrange
        var evento = new CompraCriada(Guid.NewGuid(), 1, Guid.NewGuid());
        await _outboxService.AdicionarEventoAsync(evento);
        await _context.SaveChangesAsync();
        
        var eventoSalvo = await _context.OutboxEvents.FirstAsync();
        var eventoId = eventoSalvo.Id;
        
        // Act
        await _outboxService.MarcarComoProcessadoAsync(eventoId);
        
        // Assert
        var eventoAtualizado = await _context.OutboxEvents.FindAsync(eventoId);
        eventoAtualizado.Should().NotBeNull();
        eventoAtualizado!.Status.Should().Be("Processed");
        eventoAtualizado.ProcessedAt.Should().NotBeNull();
        eventoAtualizado.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public async Task MarcarComoFalhadoAsync_DeveIncrementarRetryCountEAtualizarStatus()
    {
        // Arrange
        var evento = new CompraCriada(Guid.NewGuid(), 1, Guid.NewGuid());
        await _outboxService.AdicionarEventoAsync(evento);
        await _context.SaveChangesAsync();
        
        var eventoSalvo = await _context.OutboxEvents.FirstAsync();
        var eventoId = eventoSalvo.Id;
        var mensagemErro = "Erro ao processar evento de teste";
        
        // Act
        await _outboxService.MarcarComoFalhadoAsync(eventoId, mensagemErro);
        
        // Assert
        var eventoAtualizado = await _context.OutboxEvents.FindAsync(eventoId);
        eventoAtualizado.Should().NotBeNull();
        eventoAtualizado!.Status.Should().Be("Failed");
        eventoAtualizado.RetryCount.Should().Be(1);
        eventoAtualizado.LastError.Should().Be(mensagemErro);
    }
    
    [Fact]
    public async Task MarcarComoFalhadoAsync_DeveIncrementarRetryCountMultiplasVezes()
    {
        // Arrange
        var evento = new CompraCriada(Guid.NewGuid(), 1, Guid.NewGuid());
        await _outboxService.AdicionarEventoAsync(evento);
        await _context.SaveChangesAsync();
        
        var eventoSalvo = await _context.OutboxEvents.FirstAsync();
        var eventoId = eventoSalvo.Id;
        
        // Act - Marcar como falhado 3 vezes
        await _outboxService.MarcarComoFalhadoAsync(eventoId, "Erro 1");
        await _outboxService.MarcarComoFalhadoAsync(eventoId, "Erro 2");
        await _outboxService.MarcarComoFalhadoAsync(eventoId, "Erro 3");
        
        // Assert
        var eventoAtualizado = await _context.OutboxEvents.FindAsync(eventoId);
        eventoAtualizado.Should().NotBeNull();
        eventoAtualizado!.RetryCount.Should().Be(3);
        eventoAtualizado.LastError.Should().Be("Erro 3");
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}
