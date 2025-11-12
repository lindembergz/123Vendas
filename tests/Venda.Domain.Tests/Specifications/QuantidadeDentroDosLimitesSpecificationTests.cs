using FluentAssertions;
using Venda.Domain.Aggregates;
using Venda.Domain.Services;
using Venda.Domain.Specifications;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Tests.Specifications;

public class QuantidadeDentroDosLimitesSpecificationTests
{
    private readonly PoliticaDesconto _politicaDesconto = new();
    private readonly QuantidadeDentroDosLimitesSpecification _specification;
    
    public QuantidadeDentroDosLimitesSpecificationTests()
    {
        _specification = new QuantidadeDentroDosLimitesSpecification(_politicaDesconto);
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenQuantityExceeds20_ShouldReturnFailure()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 15, 100m));
        
        var item = new ItemVenda(produtoId, 6, 100m); // Total seria 21
        
        // Act
        var result = _specification.IsSatisfiedBy(item, venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("mais de 20 unidades");
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenQuantityWithin20_ShouldReturnSuccess()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 10, 100m));
        
        var item = new ItemVenda(produtoId, 5, 100m); // Total seria 15
        
        // Act
        var result = _specification.IsSatisfiedBy(item, venda);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenQuantityExactly20_ShouldReturnSuccess()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 15, 100m));
        
        var item = new ItemVenda(produtoId, 5, 100m); // Total seria 20
        
        // Act
        var result = _specification.IsSatisfiedBy(item, venda);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenNewProduct_ShouldValidateOnlyNewQuantity()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var item = new ItemVenda(Guid.NewGuid(), 15, 100m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, venda);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenNewProductExceeds20_ShouldReturnFailure()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var item = new ItemVenda(Guid.NewGuid(), 21, 100m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("mais de 20 unidades");
    }
}
