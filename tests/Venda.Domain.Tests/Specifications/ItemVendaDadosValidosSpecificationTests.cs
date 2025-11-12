using FluentAssertions;
using Venda.Domain.Aggregates;
using Venda.Domain.Services;
using Venda.Domain.Specifications;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Tests.Specifications;

public class ItemVendaDadosValidosSpecificationTests
{
    private readonly ItemVendaDadosValidosSpecification _specification = new();
    private readonly VendaAgregado _venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), new PoliticaDesconto());
    
    [Fact]
    public void IsSatisfiedBy_WhenProdutoIdEmpty_ShouldReturnFailure()
    {
        // Arrange
        var item = new ItemVenda(Guid.Empty, 1, 100m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, _venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("ProdutoId é obrigatório");
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenQuantityZero_ShouldReturnFailure()
    {
        // Arrange
        var item = new ItemVenda(Guid.NewGuid(), 0, 100m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, _venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantidade deve ser maior que zero");
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenQuantityNegative_ShouldReturnFailure()
    {
        // Arrange
        var item = new ItemVenda(Guid.NewGuid(), -5, 100m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, _venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantidade deve ser maior que zero");
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenValorUnitarioZero_ShouldReturnFailure()
    {
        // Arrange
        var item = new ItemVenda(Guid.NewGuid(), 1, 0m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, _venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Valor unitário deve ser maior que zero");
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenValorUnitarioNegative_ShouldReturnFailure()
    {
        // Arrange
        var item = new ItemVenda(Guid.NewGuid(), 1, -50m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, _venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Valor unitário deve ser maior que zero");
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenValorUnitarioExceedsMax_ShouldReturnFailure()
    {
        // Arrange
        var item = new ItemVenda(Guid.NewGuid(), 1, 1000000m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, _venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("menor que");
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenValidData_ShouldReturnSuccess()
    {
        // Arrange
        var item = new ItemVenda(Guid.NewGuid(), 5, 100m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, _venda);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenValorUnitarioAtMax_ShouldReturnSuccess()
    {
        // Arrange
        var item = new ItemVenda(Guid.NewGuid(), 1, 999999.99m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, _venda);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
