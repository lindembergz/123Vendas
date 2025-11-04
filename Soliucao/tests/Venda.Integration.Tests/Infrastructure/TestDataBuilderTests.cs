using FluentAssertions;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Infrastructure;

/// <summary>
/// Testes para validar o TestDataBuilder e garantir que gera dados corretos.
/// </summary>
public class TestDataBuilderTests
{
    private readonly TestDataBuilder _builder;

    public TestDataBuilderTests()
    {
        _builder = new TestDataBuilder();
    }

    [Fact]
    public void GerarVendaValida_DeveRetornarVendaComDadosValidos()
    {
        
        var venda = _builder.GerarVendaValida();

        
        venda.Should().NotBeNull();
        venda.ClienteId.Should().NotBeEmpty();
        venda.FilialId.Should().NotBeEmpty();
        venda.Itens.Should().HaveCount(1);
        venda.Itens.First().ProdutoId.Should().NotBeEmpty();
        venda.Itens.First().Quantidade.Should().BeGreaterThan(0);
        venda.Itens.First().ValorUnitario.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GerarVendaValida_ComMultiplosItens_DeveRetornarQuantidadeCorreta()
    {
        
        var quantidadeItens = 5;

        
        var venda = _builder.GerarVendaValida(quantidadeItens);

        
        venda.Itens.Should().HaveCount(quantidadeItens);
        venda.Itens.Should().OnlyContain(item => item.ProdutoId != Guid.Empty);
    }

    [Fact]
    public void GerarItens_DeveRetornarListaComQuantidadeCorreta()
    {
        
        var quantidade = 3;

        
        var itens = _builder.GerarItens(quantidade);

        
        itens.Should().HaveCount(quantidade);
        itens.Should().OnlyContain(item => 
            item.ProdutoId != Guid.Empty &&
            item.Quantidade > 0 &&
            item.ValorUnitario > 0);
    }

    [Fact]
    public void GerarItemComDesconto10_DeveRetornarItemComQuantidadeEntre4E9()
    {
        
        var item = _builder.GerarItemComDesconto10();

        
        item.Should().NotBeNull();
        item.ProdutoId.Should().NotBeEmpty();
        item.Quantidade.Should().BeInRange(4, 9);
        item.ValorUnitario.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GerarItemComDesconto20_DeveRetornarItemComQuantidadeEntre10E20()
    {
        
        var item = _builder.GerarItemComDesconto20();

        
        item.Should().NotBeNull();
        item.ProdutoId.Should().NotBeEmpty();
        item.Quantidade.Should().BeInRange(10, 20);
        item.ValorUnitario.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GerarVendaValida_DevesGerarGUIDsUnicos()
    {
        
        var venda1 = _builder.GerarVendaValida();
        var venda2 = _builder.GerarVendaValida();

        
        venda1.ClienteId.Should().NotBe(venda2.ClienteId);
        venda1.FilialId.Should().NotBe(venda2.FilialId);
        venda1.Itens.First().ProdutoId.Should().NotBe(venda2.Itens.First().ProdutoId);
    }

    [Fact]
    public void GerarItens_DeveGerarValoresMonetariosRealisticos()
    {
        
        var itens = _builder.GerarItens(10);

        
        itens.Should().OnlyContain(item => 
            item.ValorUnitario >= 10 && 
            item.ValorUnitario <= 10000);
    }
}
