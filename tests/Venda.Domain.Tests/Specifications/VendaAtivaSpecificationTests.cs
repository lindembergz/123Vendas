using FluentAssertions;
using Venda.Domain.Aggregates;
using Venda.Domain.Services;
using Venda.Domain.Specifications;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Tests.Specifications;

public class VendaAtivaSpecificationTests
{
    private readonly VendaAtivaSpecification _specification = new();
    
    [Fact]
    public void IsSatisfiedBy_WhenVendaCancelada_ShouldReturnFailure()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), new PoliticaDesconto());
        venda.Cancelar();
        var item = new ItemVenda(Guid.NewGuid(), 1, 100m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, venda);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelada");
    }
    
    [Fact]
    public void IsSatisfiedBy_WhenVendaAtiva_ShouldReturnSuccess()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), new PoliticaDesconto());
        var item = new ItemVenda(Guid.NewGuid(), 1, 100m);
        
        // Act
        var result = _specification.IsSatisfiedBy(item, venda);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
