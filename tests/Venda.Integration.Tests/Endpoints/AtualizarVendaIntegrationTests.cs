using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Venda.Application.DTOs;
using Venda.Infrastructure.Data;
using Venda.Integration.Tests.Helpers;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Endpoints;

/// <summary>
/// Testes de integração para o endpoint PUT /api/v1/vendas/{id} (Atualizar Venda).
/// Valida atualização de vendas, consolidação de itens, recálculo de descontos e geração de eventos.
/// </summary>
public class AtualizarVendaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly TestDataBuilder _builder;

    public AtualizarVendaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _builder = new TestDataBuilder();
    }

    [Fact]
    public async Task Put_VendaAtiva_DeveRetornar200EAtualizarDados()
    {
        
        var vendaId = await CriarVendaHelper();
        var novosItens = _builder.GerarItens(2);
        var request = new AtualizarVendaRequest(novosItens);

        
        var response = await _client.PutAsJsonAsync($"/api/v1/vendas/{vendaId}", request);

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var venda = await response.Content.ReadFromJsonAsync<VendaDto>();
        venda.Should().NotBeNull();
        venda!.Id.Should().Be(vendaId);
        venda.Itens.Should().HaveCount(2);
        venda.Status.Should().Be("Ativa");
    }

    [Fact]
    public async Task Put_VendaCancelada_DeveRetornar400()
    {
        
        var vendaId = await CriarVendaHelper();
        
        // Cancelar a venda
        await _client.DeleteAsync($"/api/v1/vendas/{vendaId}");
        
        // Tentar atualizar venda cancelada
        var novosItens = _builder.GerarItens(1);
        var request = new AtualizarVendaRequest(novosItens);

        
        var response = await _client.PutAsJsonAsync($"/api/v1/vendas/{vendaId}", request);

        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("cancelada");
    }

    [Fact]
    public async Task Put_ItensDuplicados_DeveConsolidarEmUmaLinha()
    {
        
        var vendaId = await CriarVendaHelper();
        var produtoId = Guid.NewGuid();
        
        // Criar dois itens com o mesmo produto
        var itens = new List<ItemVendaDto>
        {
            new ItemVendaDto(
                ProdutoId: produtoId,
                Quantidade: 3,
                ValorUnitario: 100m,
                Desconto: 0,
                Total: 0
            ),
            new ItemVendaDto(
                ProdutoId: produtoId,
                Quantidade: 2,
                ValorUnitario: 100m,
                Desconto: 0,
                Total: 0
            )
        };
        
        var request = new AtualizarVendaRequest(itens);

        
        var response = await _client.PutAsJsonAsync($"/api/v1/vendas/{vendaId}", request);

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var venda = await response.Content.ReadFromJsonAsync<VendaDto>();
        venda.Should().NotBeNull();
        venda!.Itens.Should().HaveCount(1, "itens duplicados devem ser consolidados");
        
        var itemConsolidado = venda.Itens.First();
        itemConsolidado.ProdutoId.Should().Be(produtoId);
        itemConsolidado.Quantidade.Should().Be(5, "quantidades devem ser somadas (3 + 2)");
        itemConsolidado.ValorUnitario.Should().Be(100m);
        itemConsolidado.Desconto.Should().Be(0.10m, "5 unidades devem ter 10% de desconto");
    }

    [Fact]
    public async Task Put_QuantidadeAlterada_DeveRecalcularDesconto()
    {
        
        var vendaId = await CriarVendaHelper();
        var produtoId = Guid.NewGuid();
        
        // Criar item com quantidade que aciona desconto de 10% (4-9 unidades)
        var item = new ItemVendaDto(
            ProdutoId: produtoId,
            Quantidade: 5,
            ValorUnitario: 100m,
            Desconto: 0,
            Total: 0
        );
        
        var request = new AtualizarVendaRequest(new List<ItemVendaDto> { item });

        
        var response = await _client.PutAsJsonAsync($"/api/v1/vendas/{vendaId}", request);

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var venda = await response.Content.ReadFromJsonAsync<VendaDto>();
        venda.Should().NotBeNull();
        venda!.Itens.Should().HaveCount(1);
        
        var itemAtualizado = venda.Itens.First();
        itemAtualizado.Quantidade.Should().Be(5);
        itemAtualizado.Desconto.Should().Be(0.10m, "5 unidades devem ter 10% de desconto");
        itemAtualizado.Total.Should().Be(450m, "5 * 100 * 0.90 = 450");
    }

    [Fact]
    public async Task Put_VendaInexistente_DeveRetornar404()
    {
        
        var idInexistente = Guid.NewGuid();
        var novosItens = _builder.GerarItens(1);
        var request = new AtualizarVendaRequest(novosItens);

        
        var response = await _client.PutAsJsonAsync($"/api/v1/vendas/{idInexistente}", request);

        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Put_VendaAtualizada_DeveGerarEventoCompraAlterada()
    {
        
        var vendaId = await CriarVendaHelper();
        var novosItens = _builder.GerarItens(1);
        var request = new AtualizarVendaRequest(novosItens);

        
        var response = await _client.PutAsJsonAsync($"/api/v1/vendas/{vendaId}", request);

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verificar evento no banco usando helper
        var evento = await EventValidationHelper.VerificarEventoNoBanco(
            _factory,
            "CompraAlterada",
            vendaId
        );

        // Validar estrutura completa do evento
        EventValidationHelper.ValidarEstruturaEvento(
            evento,
            "CompraAlterada",
            vendaId
        );
    }

    [Fact]
    public async Task Put_ItemRemovido_DeveGerarEventoItemCancelado()
    {
        // - Criar venda com 2 itens
        var item1 = new ItemVendaDto(Guid.NewGuid(), 2, 100m, 0, 0);
        var item2 = new ItemVendaDto(Guid.NewGuid(), 3, 150m, 0, 0);
        
        var criarRequest = new CriarVendaRequest(
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto> { item1, item2 }
        );
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/vendas", criarRequest);
        var vendaId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        
        // Atualizar venda removendo um item (mantendo apenas item1)
        var atualizarRequest = new AtualizarVendaRequest(new List<ItemVendaDto> { item1 });

        
        var response = await _client.PutAsJsonAsync($"/api/v1/vendas/{vendaId}", atualizarRequest);

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verificar evento no banco usando helper
        var evento = await EventValidationHelper.VerificarEventoComProdutoNoBanco(
            _factory,
            "ItemCancelado",
            vendaId,
            item2.ProdutoId
        );

        // Validar estrutura completa do evento
        EventValidationHelper.ValidarEstruturaEvento(
            evento,
            "ItemCancelado",
            vendaId
        );
    }

    [Fact]
    public async Task Put_VendaAtualizada_DevePersistirMudancasNoBanco()
    {
        
        var vendaId = await CriarVendaHelper();
        var produtoId = Guid.NewGuid();
        
        var novosItens = new List<ItemVendaDto>
        {
            new ItemVendaDto(
                ProdutoId: produtoId,
                Quantidade: 3,
                ValorUnitario: 200m,
                Desconto: 0,
                Total: 0
            )
        };
        
        var request = new AtualizarVendaRequest(novosItens);

        
        var response = await _client.PutAsJsonAsync($"/api/v1/vendas/{vendaId}", request);

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verificar persistência no banco
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VendaDbContext>();
        
        var vendaPersistida = await db.Vendas
            .Include(v => v.Produtos)
            .FirstOrDefaultAsync(v => v.Id == vendaId);

        vendaPersistida.Should().NotBeNull();
        vendaPersistida!.Produtos.Should().HaveCount(1);
        
        var itemPersistido = vendaPersistida.Produtos.First();
        itemPersistido.ProdutoId.Should().Be(produtoId);
        itemPersistido.Quantidade.Should().Be(3);
        itemPersistido.ValorUnitario.Should().Be(200m);
        itemPersistido.Total.Should().Be(600m, "3 * 200 = 600");
    }

    /// <summary>
    /// Método helper para criar uma venda válida e retornar seu ID.
    /// </summary>
    private async Task<Guid> CriarVendaHelper()
    {
        var request = _builder.GerarVendaValida(quantidadeItens: 2);
        var response = await _client.PostAsJsonAsync("/api/v1/vendas", request);
        response.EnsureSuccessStatusCode();
        
        var vendaId = await response.Content.ReadFromJsonAsync<Guid>();
        return vendaId;
    }
}
