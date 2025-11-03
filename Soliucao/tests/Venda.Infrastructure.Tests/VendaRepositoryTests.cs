using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Venda.Domain.Aggregates;
using Venda.Domain.Services;
using Venda.Domain.ValueObjects;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Repositories;
using Venda.Infrastructure.Services;

namespace Venda.Infrastructure.Tests;

public class VendaRepositoryTests : IDisposable
{
    private readonly VendaDbContext _context;
    private readonly VendaRepository _repository;
    
    public VendaRepositoryTests()
    {
        // Configurar banco em memória
        var options = new DbContextOptionsBuilder<VendaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new VendaDbContext(options);
        var outboxService = new OutboxService(_context);
        _repository = new VendaRepository(_context, outboxService);
    }
    
    [Fact]
    public async Task AdicionarAsync_DevePersistirVendaCorretamente()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var politicaDesconto = new PoliticaDesconto();
        var venda = VendaAgregado.Criar(clienteId, filialId, politicaDesconto);
        var item = new ItemVenda(Guid.NewGuid(), 2, 100m);
        venda.AdicionarItem(item);
        
        // Act
        await _repository.AdicionarAsync(venda);
        
        // Assert
        var vendaSalva = await _repository.ObterPorIdAsync(venda.Id);
        vendaSalva.Should().NotBeNull();
        vendaSalva!.ClienteId.Should().Be(clienteId);
        vendaSalva.FilialId.Should().Be(filialId);
        vendaSalva.Produtos.Should().HaveCount(1);
        vendaSalva.Produtos[0].ProdutoId.Should().Be(item.ProdutoId);
        vendaSalva.Produtos[0].Quantidade.Should().Be(2);
        vendaSalva.Produtos[0].ValorUnitario.Should().Be(100m);
    }
    
    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarVendaComItens()
    {
        // Arrange
        var politicaDesconto = new PoliticaDesconto();
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), politicaDesconto);
        var item1 = new ItemVenda(Guid.NewGuid(), 3, 50m);
        var item2 = new ItemVenda(Guid.NewGuid(), 1, 150m);
        venda.AdicionarItem(item1);
        venda.AdicionarItem(item2);
        
        await _repository.AdicionarAsync(venda);
        
        // Act
        var vendaRecuperada = await _repository.ObterPorIdAsync(venda.Id);
        
        // Assert
        vendaRecuperada.Should().NotBeNull();
        vendaRecuperada!.Produtos.Should().HaveCount(2);
        vendaRecuperada.ValorTotal.Should().Be(300m); // (3 * 50) + (1 * 150)
    }
    
    [Fact]
    public async Task ListarAsync_DeveRetornarTodasVendas()
    {
        // Arrange
        var politicaDesconto = new PoliticaDesconto();
        var venda1 = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), politicaDesconto);
        var venda2 = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), politicaDesconto);
        var venda3 = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), politicaDesconto);
        
        await _repository.AdicionarAsync(venda1);
        await _repository.AdicionarAsync(venda2);
        await _repository.AdicionarAsync(venda3);
        
        // Act
        var vendas = await _repository.ListarAsync();
        
        // Assert
        vendas.Should().HaveCount(3);
        vendas.Should().Contain(v => v.Id == venda1.Id);
        vendas.Should().Contain(v => v.Id == venda2.Id);
        vendas.Should().Contain(v => v.Id == venda3.Id);
    }
    
    [Fact]
    public async Task AtualizarAsync_DeveModificarVenda()
    {
        // Arrange
        var politicaDesconto = new PoliticaDesconto();
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), politicaDesconto);
        var item = new ItemVenda(Guid.NewGuid(), 1, 100m);
        venda.AdicionarItem(item);
        
        await _repository.AdicionarAsync(venda);
        
        // Act - Adicionar mais um item
        var novoItem = new ItemVenda(Guid.NewGuid(), 2, 75m);
        venda.AdicionarItem(novoItem);
        await _repository.AtualizarAsync(venda);
        
        // Assert
        var vendaAtualizada = await _repository.ObterPorIdAsync(venda.Id);
        vendaAtualizada.Should().NotBeNull();
        vendaAtualizada!.Produtos.Should().HaveCount(2);
        vendaAtualizada.ValorTotal.Should().Be(250m); // 100 + (2 * 75)
    }
    
    [Fact]
    public async Task ExisteAsync_DeveRetornarTrueQuandoVendaExiste()
    {
        // Arrange
        var politicaDesconto = new PoliticaDesconto();
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), politicaDesconto);
        await _repository.AdicionarAsync(venda);
        
        // Act
        var existe = await _repository.ExisteAsync(venda.Id);
        
        // Assert
        existe.Should().BeTrue();
    }
    
    [Fact]
    public async Task ExisteAsync_DeveRetornarFalseQuandoVendaNaoExiste()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        
        // Act
        var existe = await _repository.ExisteAsync(idInexistente);
        
        // Assert
        existe.Should().BeFalse();
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}
