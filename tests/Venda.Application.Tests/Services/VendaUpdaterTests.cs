using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Venda.Application.DTOs;
using Venda.Application.Services;
using Venda.Domain.Aggregates;
using Venda.Domain.Interfaces;
using Venda.Domain.Services;
using Venda.Domain.ValueObjects;
using Xunit;

namespace Venda.Application.Tests.Services;

public class VendaUpdaterTests
{
    private readonly IPoliticaDesconto _politicaDesconto = new PoliticaDesconto();
    private readonly VendaUpdater _updater;

    public VendaUpdaterTests()
    {
        var logger = Substitute.For<ILogger<VendaUpdater>>();
        _updater = new VendaUpdater(logger);
    }

    [Fact]
    public void AtualizarItens_AdicionandoNovoItem_DeveManterItensExistentes()
    {
        
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();

        var venda = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        venda.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        venda.ClearDomainEvents();

        var novosItens = new List<ItemVendaDto>
        {
            new ItemVendaDto(produtoId1, 2, 100m, 0m, 200m), // Existente
            new ItemVendaDto(produtoId2, 3, 50m, 0m, 150m)   // Novo
        };

        
        var result = _updater.AtualizarItens(venda, novosItens);

        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(2);
        venda.Produtos.Should().Contain(p => p.ProdutoId == produtoId1 && p.Quantidade == 2);
        venda.Produtos.Should().Contain(p => p.ProdutoId == produtoId2 && p.Quantidade == 3);
    }

    [Fact]
    public void AtualizarItens_AumentandoQuantidade_DeveAdicionarUnidades()
    {
        
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var venda = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        venda.AdicionarItem(new ItemVenda(produtoId, 2, 100m, 0m));
        venda.ClearDomainEvents();

        var novosItens = new List<ItemVendaDto>
        {
            new ItemVendaDto(produtoId, 5, 100m, 0m, 500m) // Aumentar de 2 para 5
        };

        
        var result = _updater.AtualizarItens(venda, novosItens);

        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos.First().Quantidade.Should().Be(5);
    }

    [Fact]
    public void AtualizarItens_DiminuindoQuantidade_DeveRemoverUnidades()
    {
        
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var venda = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        venda.AdicionarItem(new ItemVenda(produtoId, 5, 100m, 0m));
        venda.ClearDomainEvents();

        var novosItens = new List<ItemVendaDto>
        {
            new ItemVendaDto(produtoId, 2, 100m, 0m, 200m) // Diminuir de 5 para 2
        };

        
        var result = _updater.AtualizarItens(venda, novosItens);

        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos.First().Quantidade.Should().Be(2);
    }

    [Fact]
    public void AtualizarItens_RemovendoItem_DeveRemoverCompletamente()
    {
        
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();

        var venda = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        venda.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        venda.AdicionarItem(new ItemVenda(produtoId2, 3, 50m, 0m));
        venda.ClearDomainEvents();

        var novosItens = new List<ItemVendaDto>
        {
            new ItemVendaDto(produtoId2, 3, 50m, 0m, 150m) // Manter apenas produtoId2
        };

        
        var result = _updater.AtualizarItens(venda, novosItens);

        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos.First().ProdutoId.Should().Be(produtoId2);
    }

    [Fact]
    public void AtualizarItens_MantentoQuantidadeIgual_NaoDeveAlterarNada()
    {
        
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var venda = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        venda.AdicionarItem(new ItemVenda(produtoId, 3, 100m, 0m));
        venda.ClearDomainEvents();

        var novosItens = new List<ItemVendaDto>
        {
            new ItemVendaDto(produtoId, 3, 100m, 0m, 300m) // Mesma quantidade
        };

        
        var result = _updater.AtualizarItens(venda, novosItens);

        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos.First().Quantidade.Should().Be(3);
        venda.DomainEvents.Should().BeEmpty(); // Nenhum evento gerado
    }

    [Fact]
    public void AtualizarItens_SubstituindoTodosOsItens_DeveRemoverAntigosEAdicionarNovos()
    {
        
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();
        var produtoId3 = Guid.NewGuid();
        var produtoId4 = Guid.NewGuid();

        var venda = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        venda.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        venda.AdicionarItem(new ItemVenda(produtoId2, 1, 50m, 0m));
        venda.ClearDomainEvents();

        var novosItens = new List<ItemVendaDto>
        {
            new ItemVendaDto(produtoId3, 3, 75m, 0m, 225m),
            new ItemVendaDto(produtoId4, 2, 120m, 0m, 240m)
        };

        
        var result = _updater.AtualizarItens(venda, novosItens);

        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(2);
        venda.Produtos.Should().Contain(p => p.ProdutoId == produtoId3);
        venda.Produtos.Should().Contain(p => p.ProdutoId == produtoId4);
        venda.Produtos.Should().NotContain(p => p.ProdutoId == produtoId1);
        venda.Produtos.Should().NotContain(p => p.ProdutoId == produtoId2);
    }

    [Fact]
    public void AtualizarItens_ComMaisDe20Unidades_DeveRetornarFailure()
    {
        
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var venda = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        venda.ClearDomainEvents();

        var novosItens = new List<ItemVendaDto>
        {
            new ItemVendaDto(produtoId, 21, 100m, 0m, 2100m) // Mais de 20 unidades
        };

        
        var result = _updater.AtualizarItens(venda, novosItens);

        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("20 unidades");
    }
}
