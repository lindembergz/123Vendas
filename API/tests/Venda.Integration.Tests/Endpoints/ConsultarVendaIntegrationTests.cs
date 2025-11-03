using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Venda.Application.DTOs;
using Venda.Domain.Aggregates;
using Venda.Infrastructure.Data;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Endpoints;

/// <summary>
/// Testes de integração para o endpoint GET /api/v1/vendas/{id} (Consultar Venda por ID).
/// Valida consulta de vendas existentes, inexistentes e formato de resposta.
/// </summary>
public class ConsultarVendaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly TestDataBuilder _builder;

    public ConsultarVendaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _builder = new TestDataBuilder();
    }

    [Fact]
    public async Task Get_VendaExistente_DeveRetornar200ComDados()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();

        // Act
        var response = await _client.GetAsync($"/api/v1/vendas/{vendaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var venda = await response.Content.ReadFromJsonAsync<VendaDto>();
        venda.Should().NotBeNull();
        venda!.Id.Should().Be(vendaId);
        venda.Numero.Should().BeGreaterThan(0);
        venda.Data.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        venda.ClienteId.Should().NotBeEmpty();
        venda.FilialId.Should().NotBeEmpty();
        venda.ValorTotal.Should().BeGreaterThan(0);
        venda.Status.Should().NotBeNullOrEmpty();
        venda.Itens.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_VendaInexistente_DeveRetornar404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/vendas/{idInexistente}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Get_VendaComItens_DeveRetornarTodosItensComDescontos()
    {
        // Arrange - Criar venda com múltiplos itens, incluindo um com desconto
        var itemSemDesconto = new ItemVendaDto(
            ProdutoId: Guid.NewGuid(),
            Quantidade: 2,
            ValorUnitario: 100m,
            Desconto: 0,
            Total: 0
        );
        
        var itemComDesconto10 = _builder.GerarItemComDesconto10();
        
        var request = new CriarVendaRequest(
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto> { itemSemDesconto, itemComDesconto10 }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/vendas", request);
        var vendaId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await _client.GetAsync($"/api/v1/vendas/{vendaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var venda = await response.Content.ReadFromJsonAsync<VendaDto>();
        venda.Should().NotBeNull();
        venda!.Itens.Should().HaveCount(2);
        
        // Verificar item sem desconto
        var itemRetornado1 = venda.Itens.FirstOrDefault(i => i.Quantidade == 2);
        itemRetornado1.Should().NotBeNull();
        itemRetornado1!.Desconto.Should().Be(0m);
        itemRetornado1.ValorUnitario.Should().Be(100m);
        
        // Verificar item com desconto de 10%
        var itemRetornado2 = venda.Itens.FirstOrDefault(i => i.Quantidade >= 4 && i.Quantidade <= 9);
        itemRetornado2.Should().NotBeNull();
        itemRetornado2!.Desconto.Should().Be(0.10m);
        itemRetornado2.Total.Should().Be(itemRetornado2.Quantidade * itemRetornado2.ValorUnitario * 0.90m);
    }

    [Fact]
    public async Task Get_VendaCancelada_DeveRetornarStatusCancelada()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();
        
        // Cancelar a venda
        await _client.DeleteAsync($"/api/v1/vendas/{vendaId}");

        // Act
        var response = await _client.GetAsync($"/api/v1/vendas/{vendaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var venda = await response.Content.ReadFromJsonAsync<VendaDto>();
        venda.Should().NotBeNull();
        venda!.Status.Should().Be("Cancelada");
        venda.Id.Should().Be(vendaId);
    }

    [Fact]
    public async Task Get_VendaExistente_DeveRetornarFormatoJsonCorreto()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();

        // Act
        var response = await _client.GetAsync($"/api/v1/vendas/{vendaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        
        var venda = await response.Content.ReadFromJsonAsync<VendaDto>();
        venda.Should().NotBeNull();
        
        // Validar estrutura do JSON
        venda!.Id.Should().NotBeEmpty();
        venda.Numero.Should().BeGreaterThan(0);
        venda.Data.Should().NotBe(default(DateTime));
        venda.ClienteId.Should().NotBeEmpty();
        venda.FilialId.Should().NotBeEmpty();
        venda.ValorTotal.Should().BeGreaterThanOrEqualTo(0);
        venda.Status.Should().NotBeNullOrEmpty();
        venda.Itens.Should().NotBeNull();
        
        // Validar estrutura dos itens
        foreach (var item in venda.Itens)
        {
            item.ProdutoId.Should().NotBeEmpty();
            item.Quantidade.Should().BeGreaterThan(0);
            item.ValorUnitario.Should().BeGreaterThan(0);
            item.Desconto.Should().BeGreaterThanOrEqualTo(0);
            item.Total.Should().BeGreaterThan(0);
        }
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
