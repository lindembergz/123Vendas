using FluentAssertions;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Domain.Services;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Tests;

public class VendaAgregadoTests
{
    private readonly IPoliticaDesconto _politicaDesconto = new PoliticaDesconto();
    
    [Fact]
    public void AdicionarItem_ComMenosDe4Itens_NaoDeveAplicarDesconto()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        
        var result1 = venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        var result2 = venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        var result3 = venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        
        
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1, "itens do mesmo produto devem ser consolidados em uma linha");
        venda.Produtos[0].Quantidade.Should().Be(3);
        venda.Produtos.All(p => p.Desconto == 0m).Should().BeTrue();
        venda.ValorTotal.Should().Be(300m);
    }
    
    [Fact]
    public void AdicionarItem_Com4A9ItensIguais_DeveAplicar10PorcentoDesconto()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        
        for (int i = 0; i < 5; i++)
        {
            venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        }
        
        
        venda.Produtos.Should().HaveCount(1, "itens do mesmo produto devem ser consolidados");
        venda.Produtos[0].Quantidade.Should().Be(5);
        venda.Produtos.All(p => p.Desconto == 0.10m).Should().BeTrue();
        venda.ValorTotal.Should().Be(450m); // 5 * 100 * 0.9 = 450
    }
    
    [Fact]
    public void AdicionarItem_Com10A20ItensIguais_DeveAplicar20PorcentoDesconto()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        
        for (int i = 0; i < 15; i++)
        {
            venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        }
        
        
        venda.Produtos.Should().HaveCount(1, "itens do mesmo produto devem ser consolidados");
        venda.Produtos[0].Quantidade.Should().Be(15);
        venda.Produtos.All(p => p.Desconto == 0.20m).Should().BeTrue();
        venda.ValorTotal.Should().Be(1200m); // 15 * 100 * 0.8 = 1200
    }
    
    [Fact]
    public void AdicionarItem_ComMaisDe20ItensIguais_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        // Adiciona 20 itens
        for (int i = 0; i < 20; i++)
        {
            venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        }
        
         //- tenta adicionar o 21º item
        var result = venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("mais de 20 unidades");
        venda.Produtos.Should().HaveCount(1, "deve ter apenas 1 linha consolidada");
        venda.Produtos[0].Quantidade.Should().Be(20);
    }
    
    [Fact]
    public void AdicionarItem_ComQuantidadeMultipla_DeveAplicarDescontoCorreto()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        // - adiciona 10 itens de uma vez
        var result = venda.AdicionarItem(new ItemVenda(produtoId, 10, 100m));
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos[0].Desconto.Should().Be(0.20m);
        venda.ValorTotal.Should().Be(800m); // 10 * 100 * 0.8 = 800
    }
    
    [Fact]
    public void Cancelar_DeveAlterarStatusParaCancelado()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        
        
        var result = venda.Cancelar();
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Status.Should().Be(StatusVenda.Cancelada);
    }
    
    [Fact]
    public void Cancelar_VendaJaCancelada_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.Cancelar();
        
        
        var result = venda.Cancelar();
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("já está cancelada");
    }
    

    [Fact]
    public void ValorTotal_DeveCalcularCorretamente()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produto1 = Guid.NewGuid();
        var produto2 = Guid.NewGuid();
        
        
        venda.AdicionarItem(new ItemVenda(produto1, 2, 50m));  // 100
        venda.AdicionarItem(new ItemVenda(produto2, 3, 100m)); // 300
        
        
        venda.ValorTotal.Should().Be(400m);
    }
    
    [Fact]
    public void AdicionarItem_VendaCancelada_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.Cancelar();
        
        
        var result = venda.AdicionarItem(new ItemVenda(Guid.NewGuid(), 1, 100m));
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelada");
    }
    
    [Fact]
    public void AdicionarItem_RecalculaDescontoAoAtingir4Itens()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        // - adiciona 3 itens (sem desconto)
        venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        
        var valorAntes = venda.ValorTotal; // 300
        
        // Adiciona o 4º item (deve aplicar 10% em todos)
        venda.AdicionarItem(new ItemVenda(produtoId, 1, 100m));
        
        
        valorAntes.Should().Be(300m);
        venda.ValorTotal.Should().Be(360m); // 4 * 100 * 0.9 = 360
        venda.Produtos.Should().HaveCount(1, "deve consolidar em uma linha");
        venda.Produtos[0].Quantidade.Should().Be(4);
        venda.Produtos.All(p => p.Desconto == 0.10m).Should().BeTrue();
    }
    
    [Fact]
    public void AdicionarItem_MesmoProdutoMultiplasVezes_DeveConsolidarEmUmaLinhaComDescontoCorreto()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        // - adiciona o mesmo produto em 3 operações diferentes com quantidades variadas
        var result1 = venda.AdicionarItem(new ItemVenda(produtoId, 2, 100m)); // Total: 2 (sem desconto)
        var result2 = venda.AdicionarItem(new ItemVenda(produtoId, 3, 100m)); // Total: 5 (10% desconto)
        var result3 = venda.AdicionarItem(new ItemVenda(produtoId, 5, 100m)); // Total: 10 (20% desconto)
        
        
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        
        venda.Produtos.Should().HaveCount(1, "deve ter APENAS UMA linha para o mesmo produto");
        venda.Produtos[0].ProdutoId.Should().Be(produtoId);
        venda.Produtos[0].Quantidade.Should().Be(10, "quantidade deve ser a soma: 2 + 3 + 5 = 10");
        venda.Produtos[0].Desconto.Should().Be(0.20m, "10 unidades = 20% de desconto");
        venda.Produtos[0].ValorUnitario.Should().Be(100m);
        venda.ValorTotal.Should().Be(800m, "10 * 100 * 0.8 = 800");
    }
    
    [Fact]
    public void AdicionarItem_MesmoProdutoUltrapassandoLimite_DeveRejeitarEManterEstadoAnterior()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        // - adiciona 15 unidades primeiro
        var result1 = venda.AdicionarItem(new ItemVenda(produtoId, 15, 100m));
        var quantidadeAntes = venda.Produtos[0].Quantidade;
        var valorAntes = venda.ValorTotal;
        
        // Tenta adicionar mais 6 unidades (total seria 21 - deve falhar)
        var result2 = venda.AdicionarItem(new ItemVenda(produtoId, 6, 100m));
        
        
        result1.IsSuccess.Should().BeTrue();
        result2.IsFailure.Should().BeTrue();
        result2.Error.Should().Contain("mais de 20 unidades");
        
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos[0].Quantidade.Should().Be(15, "quantidade deve permanecer 15 após rejeição");
        venda.ValorTotal.Should().Be(valorAntes, "valor total não deve mudar após rejeição");
    }
    
    [Fact]
    public void AdicionarItem_ProdutosDiferentes_DeveTerLinhasSeparadasComDescontosIndependentes()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoA = Guid.NewGuid();
        var produtoB = Guid.NewGuid();
        var produtoC = Guid.NewGuid();
        
        //- adiciona produtos diferentes com quantidades que geram descontos diferentes
        venda.AdicionarItem(new ItemVenda(produtoA, 5, 100m));  // 5 unidades = 10% desconto
        venda.AdicionarItem(new ItemVenda(produtoB, 12, 50m));  // 12 unidades = 20% desconto
        venda.AdicionarItem(new ItemVenda(produtoC, 2, 75m));   // 2 unidades = sem desconto
        
        
        venda.Produtos.Should().HaveCount(3, "deve ter 3 linhas para 3 produtos diferentes");
        
        var itemA = venda.Produtos.First(p => p.ProdutoId == produtoA);
        itemA.Quantidade.Should().Be(5);
        itemA.Desconto.Should().Be(0.10m);
        itemA.Total.Should().Be(450m); // 5 * 100 * 0.9
        
        var itemB = venda.Produtos.First(p => p.ProdutoId == produtoB);
        itemB.Quantidade.Should().Be(12);
        itemB.Desconto.Should().Be(0.20m);
        itemB.Total.Should().Be(480m); // 12 * 50 * 0.8
        
        var itemC = venda.Produtos.First(p => p.ProdutoId == produtoC);
        itemC.Quantidade.Should().Be(2);
        itemC.Desconto.Should().Be(0m);
        itemC.Total.Should().Be(150m); // 2 * 75 * 1.0
        
        venda.ValorTotal.Should().Be(1080m); // 450 + 480 + 150
    }
    
    [Fact]
    public void RemoverItem_ComQuantidadeParcial_DeveReduzirQuantidadeERecalcularDesconto()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 10, 100m)); // 10 unidades = 20% desconto
        
        // - remove 3 unidades
        var result = venda.RemoverItem(produtoId, 3);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos[0].Quantidade.Should().Be(7, "10 - 3 = 7");
        venda.Produtos[0].Desconto.Should().Be(0.10m, "7 unidades = 10% desconto");
        venda.ValorTotal.Should().Be(630m); // 7 * 100 * 0.9 = 630
    }
    
    [Fact]
    public void RemoverItem_ComQuantidadeTotal_DeveRemoverItemCompletamente()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 5, 100m));
        
        // - remove todas as 5 unidades
        var result = venda.RemoverItem(produtoId, 5);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().BeEmpty("item deve ser removido completamente");
        venda.ValorTotal.Should().Be(0m);
    }
    
    [Fact]
    public void RemoverItem_ComQuantidadeMaiorQueDisponivel_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 5, 100m));
        
         //- tenta remover 10 unidades (só tem 5)
        var result = venda.RemoverItem(produtoId, 10);
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("maior que a quantidade disponível");
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos[0].Quantidade.Should().Be(5, "quantidade não deve mudar");
    }
    
    [Fact]
    public void RemoverItem_ComQuantidadeZero_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 5, 100m));
        
        
        var result = venda.RemoverItem(produtoId, 0);
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("deve ser maior que zero");
    }
    
    [Fact]
    public void RemoverItem_ComQuantidadeNegativa_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 5, 100m));
        
        
        var result = venda.RemoverItem(produtoId, -3);
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("deve ser maior que zero");
    }
    
    [Fact]
    public void RemoverItem_ProdutoInexistente_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        
        var result = venda.RemoverItem(produtoId, 1);
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("não encontrado");
    }
    
    [Fact]
    public void RemoverItem_VendaCancelada_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 5, 100m));
        venda.Cancelar();
        
        
        var result = venda.RemoverItem(produtoId, 2);
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelada");
    }
    
    [Fact]
    public void RemoverItem_ReduzindoDe10Para3_DeveRemoverDescontoCompletamente()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 10, 100m)); // 10 unidades = 20% desconto
        
        // - remove 7 unidades, ficando com 3
        var result = venda.RemoverItem(produtoId, 7);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos[0].Quantidade.Should().Be(3);
        venda.Produtos[0].Desconto.Should().Be(0m, "3 unidades = sem desconto");
        venda.ValorTotal.Should().Be(300m); // 3 * 100 * 1.0 = 300
    }
    
    [Fact]
    public void RemoverItem_SemParametroQuantidade_DeveRemoverItemCompletamente()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 10, 100m));
        
        // - remove o item completamente (sem especificar quantidade)
        var result = venda.RemoverItem(produtoId);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().BeEmpty("item deve ser removido completamente");
        venda.ValorTotal.Should().Be(0m);
    }
    
    [Fact]
    public void AtualizarItens_AdicionandoNovoItem_DeveManterItensExistentes()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId1, 2, 100m));
        venda.ClearDomainEvents();
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(produtoId1, 2, 100m),
            new ItemVenda(produtoId2, 3, 50m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(2);
        venda.Produtos.Should().Contain(p => p.ProdutoId == produtoId1 && p.Quantidade == 2);
        venda.Produtos.Should().Contain(p => p.ProdutoId == produtoId2 && p.Quantidade == 3);
    }
    
    [Fact]
    public void AtualizarItens_AumentandoQuantidade_DeveAdicionarUnidades()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 2, 100m));
        venda.ClearDomainEvents();
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(produtoId, 5, 100m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos.First().Quantidade.Should().Be(5);
    }
    
    [Fact]
    public void AtualizarItens_DiminuindoQuantidade_DeveRemoverUnidades()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 5, 100m));
        venda.ClearDomainEvents();
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(produtoId, 2, 100m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos.First().Quantidade.Should().Be(2);
    }
    
    [Fact]
    public void AtualizarItens_RemovendoItem_DeveRemoverCompletamente()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId1, 2, 100m));
        venda.AdicionarItem(new ItemVenda(produtoId2, 3, 50m));
        venda.ClearDomainEvents();
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(produtoId2, 3, 50m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos.First().ProdutoId.Should().Be(produtoId2);
    }
    
    [Fact]
    public void AtualizarItens_MantentoQuantidadeIgual_NaoDeveAlterarNada()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId, 3, 100m));
        venda.ClearDomainEvents();
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(produtoId, 3, 100m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1);
        venda.Produtos.First().Quantidade.Should().Be(3);
        venda.DomainEvents.Should().BeEmpty();
    }
    
    [Fact]
    public void AtualizarItens_SubstituindoTodosOsItens_DeveRemoverAntigosEAdicionarNovos()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();
        var produtoId3 = Guid.NewGuid();
        var produtoId4 = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId1, 2, 100m));
        venda.AdicionarItem(new ItemVenda(produtoId2, 1, 50m));
        venda.ClearDomainEvents();
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(produtoId3, 3, 75m),
            new ItemVenda(produtoId4, 2, 120m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
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
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(Guid.NewGuid(), 21, 100m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("20 unidades");
    }
    
    [Fact]
    public void AtualizarItens_ConsolidandoItensDuplicados_DeveSomarQuantidades()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        var produtoId = Guid.NewGuid();
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(produtoId, 2, 100m),
            new ItemVenda(produtoId, 3, 100m),
            new ItemVenda(produtoId, 1, 100m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
        result.IsSuccess.Should().BeTrue();
        venda.Produtos.Should().HaveCount(1, "itens duplicados devem ser consolidados");
        venda.Produtos.First().Quantidade.Should().Be(6, "2 + 3 + 1 = 6");
        venda.Produtos.First().Desconto.Should().Be(0.10m, "6 unidades = 10% desconto");
    }
    
    [Fact]
    public void AtualizarItens_VendaCancelada_DeveRetornarFailure()
    {
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.Cancelar();
        
        var novosItens = new List<ItemVenda>
        {
            new ItemVenda(Guid.NewGuid(), 1, 100m)
        };
        
        
        var result = venda.AtualizarItens(novosItens);
        
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelada");
    }
}
