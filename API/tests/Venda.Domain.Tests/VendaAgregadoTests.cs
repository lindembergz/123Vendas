using FluentAssertions;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Tests;

public class VendaAgregadoTests
{
    [Fact]
    public void AdicionarItem_ComMenosDe4Itens_NaoDeveAplicarDesconto()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        var produtoId = Guid.NewGuid();
        
        // Act
        var result1 = venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        var result2 = venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        var result3 = venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        
        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(3);
        venda.Produtos.All(p => p.Desconto == 0m).Should().BeTrue();
        venda.ValorTotal.Should().Be(300m);
    }
    
    [Fact]
    public void AdicionarItem_Com4A9ItensIguais_DeveAplicar10PorcentoDesconto()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        var produtoId = Guid.NewGuid();
        
        // Act
        for (int i = 0; i < 5; i++)
        {
            venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        }
        
        // Assert
        venda.Produtos.Should().HaveCount(5);
        venda.Produtos.All(p => p.Desconto == 0.10m).Should().BeTrue();
        venda.ValorTotal.Should().Be(450m); // 5 * 100 * 0.9 = 450
    }
    
    [Fact]
    public void AdicionarItem_Com10A20ItensIguais_DeveAplicar20PorcentoDesconto()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        var produtoId = Guid.NewGuid();
        
        // Act
        for (int i = 0; i < 15; i++)
        {
            venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        }
        
        // Assert
        venda.Produtos.Should().HaveCount(15);
        venda.Produtos.All(p => p.Desconto == 0.20m).Should().BeTrue();
        venda.ValorTotal.Should().Be(1200m); // 15 * 100 * 0.8 = 1200
    }
    
    [Fact]
    public void AdicionarItem_ComMaisDe20ItensIguais_DeveRetornarFailure()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        var produtoId = Guid.NewGuid();
        
        // Adiciona 20 itens
        for (int i = 0; i < 20; i++)
        {
            venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        }
        
        // Act - tenta adicionar o 21º item
        var result = venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("mais de 20 unidades");
        venda.Produtos.Should().HaveCount(20);
    }
    
    [Fact]
    public void AdicionarItem_ComQuantidadeMultipla_DeveAplicarDescontoCorreto()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        var produtoId = Guid.NewGuid();
        
        // Act - adiciona 10 itens de uma vez
        var result = venda.AdicionarItem(new ItemVenda(produtoId, 10, 100m));
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos[0].Desconto.Should().Be(0.20m);
        venda.ValorTotal.Should().Be(800m); // 10 * 100 * 0.8 = 800
    }
    
    [Fact]
    public void Cancelar_DeveAlterarStatusParaCancelado()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        
        // Act
        var result = venda.Cancelar();
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        venda.Status.Should().Be(StatusVenda.Cancelada);
    }
    
    [Fact]
    public void Cancelar_VendaJaCancelada_DeveRetornarFailure()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        venda.Cancelar();
        
        // Act
        var result = venda.Cancelar();
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("já está cancelada");
    }
    
    [Fact]
    public void MarcarComoPendenteValidacao_DeveAlterarStatus()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        
        // Act
        venda.MarcarComoPendenteValidacao();
        
        // Assert
        venda.Status.Should().Be(StatusVenda.PendenteValidacao);
    }
    
    [Fact]
    public void ValorTotal_DeveCalcularCorretamente()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        var produto1 = Guid.NewGuid();
        var produto2 = Guid.NewGuid();
        
        // Act
        venda.AdicionarItem(new ItemVenda(produto1, 2, 50m));  // 100
        venda.AdicionarItem(new ItemVenda(produto2, 3, 100m)); // 300
        
        // Assert
        venda.ValorTotal.Should().Be(400m);
    }
    
    [Fact]
    public void AdicionarItem_VendaCancelada_DeveRetornarFailure()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        venda.Cancelar();
        
        // Act
        var result = venda.AdicionarItem(new ItemVenda(Guid.NewGuid(), 1, 100m));
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelada");
    }
    
    [Fact]
    public void AdicionarItem_RecalculaDescontoAoAtingir4Itens()
    {
        // Arrange
        var venda = VendaAgregado.Criar(Guid.NewGuid(), "Filial1");
        var produtoId = Guid.NewGuid();
        
        // Act - adiciona 3 itens (sem desconto)
        venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        
        var valorAntes = venda.ValorTotal; // 300
        
        // Adiciona o 4º item (deve aplicar 10% em todos)
        venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        
        // Assert
        valorAntes.Should().Be(300m);
        venda.ValorTotal.Should().Be(360m); // 4 * 100 * 0.9 = 360
        venda.Produtos.All(p => p.Desconto == 0.10m).Should().BeTrue();
    }
}
