using FluentAssertions;
using Venda.Domain.Exceptions;
using Venda.Domain.Services;

namespace Venda.Domain.Tests;

public class PoliticaDescontoTests
{
    private readonly PoliticaDesconto _politica = new();

    [Theory]
    [InlineData(1, 0.00)]
    [InlineData(2, 0.00)]
    [InlineData(3, 0.00)]
    public void Calcular_QuantidadeMenorQue4_DeveRetornarSemDesconto(int quantidade, decimal descontoEsperado)
    {
        
        var desconto = _politica.Calcular(quantidade);

        
        desconto.Should().Be(descontoEsperado);
    }

    [Theory]
    [InlineData(4, 0.10)]
    [InlineData(5, 0.10)]
    [InlineData(7, 0.10)]
    [InlineData(9, 0.10)]
    public void Calcular_QuantidadeEntre4E9_DeveRetornar10PorcentoDesconto(int quantidade, decimal descontoEsperado)
    {
        
        var desconto = _politica.Calcular(quantidade);

        
        desconto.Should().Be(descontoEsperado);
    }

    [Theory]
    [InlineData(10, 0.20)]
    [InlineData(15, 0.20)]
    [InlineData(20, 0.20)]
    public void Calcular_QuantidadeEntre10E20_DeveRetornar20PorcentoDesconto(int quantidade, decimal descontoEsperado)
    {
        
        var desconto = _politica.Calcular(quantidade);

        
        desconto.Should().Be(descontoEsperado);
    }

    [Fact]
    public void Calcular_QuantidadeMaiorQue20_DeveLancarDomainException()
    {
        
        var quantidade = 21;

        
        var act = () => _politica.Calcular(quantidade);

        
        act.Should().Throw<DomainException>()
            .WithMessage("*20 unidades*");
    }

    [Theory]
    [InlineData(25)]
    [InlineData(30)]
    [InlineData(100)]
    public void Calcular_QuantidadesMuitoAcimaDe20_DeveLancarDomainException(int quantidade)
    {
        
        var act = () => _politica.Calcular(quantidade);

        
        act.Should().Throw<DomainException>()
            .WithMessage("Não é permitido vender mais de 20 unidades do mesmo produto.");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(20, true)]
    public void PermiteVenda_QuantidadeAte20_DeveRetornarTrue(int quantidade, bool esperado)
    {
        
        var resultado = _politica.PermiteVenda(quantidade);

        
        resultado.Should().Be(esperado);
    }

    [Theory]
    [InlineData(21, false)]
    [InlineData(25, false)]
    [InlineData(100, false)]
    public void PermiteVenda_QuantidadeMaiorQue20_DeveRetornarFalse(int quantidade, bool esperado)
    {
        
        var resultado = _politica.PermiteVenda(quantidade);

        
        resultado.Should().Be(esperado);
    }

    [Fact]
    public void PoliticaDesconto_DeveSerTestadaIsoladamente()
    {
        
        var politica = new PoliticaDesconto();

        // & Assert - Testa todos os cenários em um único teste
        politica.Calcular(3).Should().Be(0m, "quantidade menor que 4 não tem desconto");
        politica.Calcular(5).Should().Be(0.10m, "quantidade entre 4 e 9 tem 10% de desconto");
        politica.Calcular(15).Should().Be(0.20m, "quantidade entre 10 e 20 tem 20% de desconto");

        var act = () => politica.Calcular(21);
        act.Should().Throw<DomainException>()
            .WithMessage("*20 unidades*", "quantidade acima de 20 não é permitida");
    }
}
