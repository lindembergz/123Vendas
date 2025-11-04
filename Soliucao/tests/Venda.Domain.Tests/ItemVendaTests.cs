using FluentAssertions;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Tests;

public class ItemVendaTests
{
    [Fact]
    public void Total_SemDesconto_DeveCalcularCorretamente()
    {
        
        var produtoId = Guid.NewGuid();
        var item = new ItemVenda(produtoId, 5, 100m, 0m);
        
        
        var total = item.Total;
        
        
        total.Should().Be(500m);
    }
    
    [Fact]
    public void Total_ComDesconto10Porcento_DeveCalcularCorretamente()
    {
        
        var produtoId = Guid.NewGuid();
        var item = new ItemVenda(produtoId, 5, 100m, 0.10m);
        
        
        var total = item.Total;
        
        
        total.Should().Be(450m); // 5 * 100 * (1 - 0.10) = 450
    }
    
    [Fact]
    public void Total_ComDesconto20Porcento_DeveCalcularCorretamente()
    {
        
        var produtoId = Guid.NewGuid();
        var item = new ItemVenda(produtoId, 10, 100m, 0.20m);
        
        
        var total = item.Total;
        
        
        total.Should().Be(800m); // 10 * 100 * (1 - 0.20) = 800
    }
    
    [Fact]
    public void WithDesconto_DeveRetornarNovoItemVenda_Imutabilidade()
    {
        
        var produtoId = Guid.NewGuid();
        var itemOriginal = new ItemVenda(produtoId, 5, 100m, 0m);
        
        
        var itemComDesconto = itemOriginal.WithDesconto(0.10m);
        
        
        itemComDesconto.Should().NotBeSameAs(itemOriginal);
        itemOriginal.Desconto.Should().Be(0m);
        itemComDesconto.Desconto.Should().Be(0.10m);
        itemComDesconto.ProdutoId.Should().Be(produtoId);
        itemComDesconto.Quantidade.Should().Be(5);
        itemComDesconto.ValorUnitario.Should().Be(100m);
    }
}
