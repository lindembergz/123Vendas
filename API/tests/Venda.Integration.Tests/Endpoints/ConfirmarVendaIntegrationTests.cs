using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Venda.Application.DTOs;
using Venda.Domain.Enums;
using Venda.Infrastructure.Data;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Endpoints;

/// <summary>
/// Testes de integração para o endpoint POST /api/v1/vendas/{id}/confirmar (Confirmar Venda).
/// Valida confirmação de vendas pendentes, transição de status e validações de regras de negócio.
/// </summary>
public class ConfirmarVendaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly TestDataBuilder _builder;

    public ConfirmarVendaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _builder = new TestDataBuilder();
    }

    [Fact]
    public async Task Post_VendaPendente_DeveRetornar200EConfirmar()
    {
        // Arrange
        var vendaId = await CriarVendaPendenteHelper();

        // Act
        var response = await _client.PostAsync($"/api/v1/vendas/{vendaId}/confirmar", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var vendaConfirmada = await response.Content.ReadFromJsonAsync<VendaDto>();
        vendaConfirmada.Should().NotBeNull();
        vendaConfirmada!.Id.Should().Be(vendaId);
        vendaConfirmada.Status.Should().Be("Ativa", "venda confirmada deve ter status Ativa");
    }

    [Fact]
    public async Task Post_VendaAtiva_DeveRetornar400()
    {
        // Arrange
        var vendaId = await CriarVendaHelper(); // Cria venda já ativa

        // Act
        var response = await _client.PostAsync($"/api/v1/vendas/{vendaId}/confirmar", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("Ativa", "deve indicar que a venda já está ativa");
    }

    [Fact]
    public async Task Post_VendaCancelada_DeveRetornar400()
    {
        // Arrange
        var vendaId = await CriarVendaHelper();
        await _client.DeleteAsync($"/api/v1/vendas/{vendaId}"); // Cancelar venda

        // Act
        var response = await _client.PostAsync($"/api/v1/vendas/{vendaId}/confirmar", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("Cancelada", "deve indicar que não é possível confirmar venda cancelada");
    }

    [Fact]
    public async Task Post_VendaInexistente_DeveRetornar404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/vendas/{idInexistente}/confirmar", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("não encontrada", "deve indicar que a venda não existe");
    }

    [Fact]
    public async Task Post_VendaConfirmada_DeveAlterarStatusParaAtiva()
    {
        // Arrange
        var vendaId = await CriarVendaPendenteHelper();

        // Act
        var response = await _client.PostAsync($"/api/v1/vendas/{vendaId}/confirmar", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verificar status no banco
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VendaDbContext>();
        
        var vendaConfirmada = await db.Vendas
            .FirstOrDefaultAsync(v => v.Id == vendaId);

        vendaConfirmada.Should().NotBeNull();
        vendaConfirmada!.Status.Should().Be(StatusVenda.Ativa, "o status deve ser alterado para Ativa após confirmação");
    }

    /// <summary>
    /// Método helper para criar uma venda válida com status Ativa para uso nos testes.
    /// </summary>
    private async Task<Guid> CriarVendaHelper()
    {
        var request = _builder.GerarVendaValida(quantidadeItens: 2);
        var response = await _client.PostAsJsonAsync("/api/v1/vendas", request);
        response.EnsureSuccessStatusCode();
        
        var vendaId = await response.Content.ReadFromJsonAsync<Guid>();
        return vendaId;
    }

    /// <summary>
    /// Método helper para criar uma venda com status PendenteValidacao.
    /// Como não há endpoint para criar vendas pendentes, manipulamos diretamente o banco de dados.
    /// </summary>
    private async Task<Guid> CriarVendaPendenteHelper()
    {
        // Primeiro criar uma venda normal (status Ativa)
        var vendaId = await CriarVendaHelper();

        // Depois alterar o status diretamente no banco para PendenteValidacao
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VendaDbContext>();
        
        var venda = await db.Vendas.FirstOrDefaultAsync(v => v.Id == vendaId);
        if (venda != null)
        {
            venda.MarcarComoPendenteValidacao();
            await db.SaveChangesAsync();
        }

        return vendaId;
    }
}
