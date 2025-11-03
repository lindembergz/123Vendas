using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Venda.Application.DTOs;
using Venda.Domain.Enums;
using Venda.Infrastructure.Data;
using Venda.Integration.Tests.Helpers;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Endpoints;

/// <summary>
/// Testes de integração para o endpoint DELETE /api/v1/vendas/{id} (Cancelar Venda).
/// Valida cancelamento de vendas (soft delete), geração de eventos e manutenção de dados históricos.
/// </summary>
public class CancelarVendaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly TestDataBuilder _builder;

    public CancelarVendaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _builder = new TestDataBuilder();
    }

    [Fact]
    public async Task Delete_VendaAtiva_DeveRetornar204()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/vendas/{vendaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_VendaJaCancelada_DeveRetornar400()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();
        await _client.DeleteAsync($"/api/v1/vendas/{vendaId}"); // Primeira vez - cancela

        // Act
        var response = await _client.DeleteAsync($"/api/v1/vendas/{vendaId}"); // Segunda vez - deve falhar

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("cancelada", "deve indicar que a venda já está cancelada");
    }

    [Fact]
    public async Task Delete_VendaInexistente_DeveRetornar404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/vendas/{idInexistente}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("não encontrada", "deve indicar que a venda não existe");
    }

    [Fact]
    public async Task Delete_VendaCancelada_DeveAlterarStatusNoBanco()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/vendas/{vendaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar status no banco
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VendaDbContext>();
        
        var vendaCancelada = await db.Vendas
            .FirstOrDefaultAsync(v => v.Id == vendaId);

        vendaCancelada.Should().NotBeNull();
        vendaCancelada!.Status.Should().Be(StatusVenda.Cancelada, "o status deve ser alterado para Cancelada");
    }

    [Fact]
    public async Task Delete_VendaCancelada_DeveGerarEventoCompraCancelada()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/vendas/{vendaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar evento no banco usando helper
        var evento = await EventValidationHelper.VerificarEventoNoBanco(
            _factory,
            "CompraCancelada",
            vendaId
        );

        // Validar estrutura completa do evento
        EventValidationHelper.ValidarEstruturaEvento(
            evento,
            "CompraCancelada",
            vendaId
        );
    }

    [Fact]
    public async Task Delete_VendaCancelada_DeveManterDadosHistoricos()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();
        
        // Buscar dados originais antes do cancelamento
        var vendaOriginal = await _client.GetFromJsonAsync<VendaDto>($"/api/v1/vendas/{vendaId}");
        vendaOriginal.Should().NotBeNull();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/vendas/{vendaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar que a venda ainda pode ser consultada (soft delete)
        var vendaResponse = await _client.GetAsync($"/api/v1/vendas/{vendaId}");
        vendaResponse.StatusCode.Should().Be(HttpStatusCode.OK, "venda cancelada deve permanecer consultável");

        var vendaCancelada = await vendaResponse.Content.ReadFromJsonAsync<VendaDto>();
        vendaCancelada.Should().NotBeNull();
        vendaCancelada!.Id.Should().Be(vendaId);
        vendaCancelada.Status.Should().Be("Cancelada");
        
        // Verificar que os dados históricos foram mantidos
        vendaCancelada.ClienteId.Should().Be(vendaOriginal!.ClienteId);
        vendaCancelada.FilialId.Should().Be(vendaOriginal.FilialId);
        vendaCancelada.Numero.Should().Be(vendaOriginal.Numero);
        vendaCancelada.Itens.Should().HaveCount(vendaOriginal.Itens.Count);
        vendaCancelada.ValorTotal.Should().Be(vendaOriginal.ValorTotal);
    }

    /// <summary>
    /// Método helper para criar uma venda válida para uso nos testes.
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
