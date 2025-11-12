using Venda.Application.DTOs;
using Venda.Application.Mappers;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Domain.Services;
using Venda.Domain.ValueObjects;

namespace Venda.Application.Tests.Mappers;

public class VendaMapperTests
{
    private readonly IPoliticaDesconto _politicaDesconto = new PoliticaDesconto();

    [Fact]
    public void ToDto_WithValidVenda_ShouldMapAllProperties()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var numeroVenda = 12345;

        var venda = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        venda.DefinirNumeroVenda(numeroVenda);

        var item = new ItemVenda(produtoId, 5, 100.00m, 10.00m);
        venda.AdicionarItem(item);

        // Act
        var dto = venda.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(venda.Id, dto.Id);
        Assert.Equal(numeroVenda, dto.Numero);
        Assert.Equal(venda.Data, dto.Data);
        Assert.Equal(clienteId, dto.ClienteId);
        Assert.Equal(filialId, dto.FilialId);
        Assert.Equal(StatusVenda.Ativa.ToString(), dto.Status);
        Assert.Single(dto.Itens);
    }

    [Fact]
    public void ToDto_WithMultipleItems_ShouldMapAllItems()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.DefinirNumeroVenda(1);

        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();
        var produtoId3 = Guid.NewGuid();

        var item1 = new ItemVenda(produtoId1, 2, 50.00m);
        var item2 = new ItemVenda(produtoId2, 3, 75.00m);
        var item3 = new ItemVenda(produtoId3, 1, 100.00m);

        venda.AdicionarItem(item1);
        venda.AdicionarItem(item2);
        venda.AdicionarItem(item3);

        // Act
        var dto = venda.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(3, dto.Itens.Count);
        
        var dtoItem1 = dto.Itens[0];
        Assert.Equal(produtoId1, dtoItem1.ProdutoId);
        Assert.Equal(2, dtoItem1.Quantidade);
        Assert.Equal(50.00m, dtoItem1.ValorUnitario);
    }

    [Fact]
    public void ToDto_WithCancelledVenda_ShouldMapStatusCorrectly()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.DefinirNumeroVenda(1);

        var item = new ItemVenda(Guid.NewGuid(), 1, 100.00m, 0.00m);
        venda.AdicionarItem(item);
        venda.Cancelar();

        // Act
        var dto = venda.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(StatusVenda.Cancelada.ToString(), dto.Status);
    }

    [Fact]
    public void ToDto_ShouldCalculateValorTotalCorrectly()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.DefinirNumeroVenda(1);

        // Item 1: 2 * 50.00 = 100.00 (no discount for < 4 items)
        var item1 = new ItemVenda(Guid.NewGuid(), 2, 50.00m);
        // Item 2: 3 * 75.00 = 225.00 (no discount for < 4 items)
        var item2 = new ItemVenda(Guid.NewGuid(), 3, 75.00m);
        
        venda.AdicionarItem(item1);
        venda.AdicionarItem(item2);

        // Act
        var dto = venda.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(325.00m, dto.ValorTotal); // 100.00 + 225.00
    }

    [Fact]
    public void ToDto_WithNullVenda_ShouldThrowArgumentNullException()
    {
        // Arrange
        VendaAgregado? venda = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => venda!.ToDto());
        Assert.Equal("venda", exception.ParamName);
    }

    [Fact]
    public void ToDto_WithEmptyItems_ShouldReturnEmptyItemsList()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.DefinirNumeroVenda(1);

        // Act
        var dto = venda.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Empty(dto.Itens);
        Assert.Equal(0m, dto.ValorTotal);
    }

    [Fact]
    public void ToDto_WithCollection_ShouldMapAllVendas()
    {
        // Arrange
        var venda1 = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda1.DefinirNumeroVenda(1);

        var venda2 = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda2.DefinirNumeroVenda(2);

        var venda3 = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda3.DefinirNumeroVenda(3);

        var vendas = new List<VendaAgregado> { venda1, venda2, venda3 };

        // Act
        var dtos = vendas.ToDto();

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(3, dtos.Count);
        Assert.Equal(venda1.Id, dtos[0].Id);
        Assert.Equal(venda2.Id, dtos[1].Id);
        Assert.Equal(venda3.Id, dtos[2].Id);
    }

    [Fact]
    public void ToDto_WithEmptyCollection_ShouldReturnEmptyList()
    {
        // Arrange
        var vendas = new List<VendaAgregado>();

        // Act
        var dtos = vendas.ToDto();

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }

    [Fact]
    public void ToDto_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<VendaAgregado>? vendas = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => vendas!.ToDto());
        Assert.Equal("vendas", exception.ParamName);
    }

    [Fact]
    public void ToDto_ItemDto_ShouldMapAllItemProperties()
    {
        // Arrange
        var produtoId = Guid.NewGuid();
        var quantidade = 10;
        var valorUnitario = 150.75m;

        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.DefinirNumeroVenda(1);

        var item = new ItemVenda(produtoId, quantidade, valorUnitario);
        venda.AdicionarItem(item);

        // Act
        var dto = venda.ToDto();
        var itemDto = dto.Itens.First();

        // Assert
        Assert.Equal(produtoId, itemDto.ProdutoId);
        Assert.Equal(quantidade, itemDto.Quantidade);
        Assert.Equal(valorUnitario, itemDto.ValorUnitario);
        // Discount is calculated by PoliticaDesconto (10-19 items = 20%)
        Assert.Equal(0.20m, itemDto.Desconto);
        Assert.Equal(venda.Produtos.First().Total, itemDto.Total);
    }

    [Fact]
    public void ToDto_WithLargeCollection_ShouldMapAllVendasEfficiently()
    {
        // Arrange
        var vendas = new List<VendaAgregado>();
        for (int i = 0; i < 100; i++)
        {
            var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
            venda.DefinirNumeroVenda(i + 1);

            var item = new ItemVenda(Guid.NewGuid(), 1, 100.00m, 0.00m);
            venda.AdicionarItem(item);
            
            vendas.Add(venda);
        }

        // Act
        var dtos = vendas.ToDto();

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(100, dtos.Count);
        Assert.All(dtos, dto => Assert.Single(dto.Itens));
    }
}
