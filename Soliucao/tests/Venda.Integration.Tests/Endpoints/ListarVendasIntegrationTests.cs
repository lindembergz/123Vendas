using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Venda.Application.DTOs;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Endpoints;

/// <summary>
/// Testes de integração para o endpoint GET /api/v1/vendas (Listar Vendas).
/// Valida listagem com filtros, paginação e metadados.
/// </summary>
public class ListarVendasIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly TestDataBuilder _builder;

    public ListarVendasIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _builder = new TestDataBuilder();
    }

    [Fact]
    public async Task Get_SemFiltros_DeveRetornarTodasVendasPaginadas()
    {
        
        await CriarMultiplasVendas(5);

        
        var response = await _client.GetAsync("/api/v1/vendas?pageNumber=1&pageSize=10");

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterThanOrEqualTo(5, "deve ter pelo menos as 5 vendas criadas neste teste");
        result.TotalCount.Should().BeGreaterThanOrEqualTo(5, "deve ter pelo menos as 5 vendas criadas neste teste");
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ComFiltroClienteId_DeveRetornarApenasVendasDoCliente()
    {
        
        var clienteId = Guid.NewGuid();
        await CriarVendaComCliente(clienteId);
        await CriarVendaComCliente(clienteId);
        await CriarVendaComCliente(Guid.NewGuid()); // Venda de outro cliente

        
        var response = await _client.GetAsync($"/api/v1/vendas?pageNumber=1&pageSize=10&clienteId={clienteId}");

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(v => v.ClienteId == clienteId);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Get_ComFiltroFilialId_DeveRetornarApenasVendasDaFilial()
    {
        
        var filialId = Guid.NewGuid();
        await CriarVendaComFilial(filialId);
        await CriarVendaComFilial(filialId);
        await CriarVendaComFilial(Guid.NewGuid()); // Venda de outra filial

        
        var response = await _client.GetAsync($"/api/v1/vendas?pageNumber=1&pageSize=10&filialId={filialId}");

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(v => v.FilialId == filialId);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Get_ComFiltroStatus_DeveRetornarApenasVendasComAqueleStatus()
    {
        
        var vendaId1 = await CriarVendaHelper();
        var vendaId2 = await CriarVendaHelper();
        var vendaId3 = await CriarVendaHelper();
        
        // Cancelar uma venda
        await _client.DeleteAsync($"/api/v1/vendas/{vendaId3}");

         - Buscar vendas ativas
        var responseAtivas = await _client.GetAsync("/api/v1/vendas?pageNumber=1&pageSize=10&status=Ativa");
        var responseCanceladas = await _client.GetAsync("/api/v1/vendas?pageNumber=1&pageSize=10&status=Cancelada");

         - Vendas ativas
        responseAtivas.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultAtivas = await responseAtivas.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();
        resultAtivas.Should().NotBeNull();
        resultAtivas!.Items.Should().HaveCountGreaterThanOrEqualTo(2, "deve ter pelo menos as 2 vendas ativas criadas neste teste");
        resultAtivas.Items.Should().OnlyContain(v => v.Status == "Ativa");
        
         - Vendas canceladas
        responseCanceladas.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultCanceladas = await responseCanceladas.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();
        resultCanceladas.Should().NotBeNull();
        resultCanceladas!.Items.Should().HaveCountGreaterThanOrEqualTo(1, "deve ter pelo menos a venda cancelada criada neste teste");
        resultCanceladas.Items.Should().OnlyContain(v => v.Status == "Cancelada");
    }

    [Fact]
    public async Task Get_ComPaginacao_DeveRespeitarPageNumberEPageSize()
    {
        
        await CriarMultiplasVendas(15);

         - Primeira página com 5 itens
        var responsePage1 = await _client.GetAsync("/api/v1/vendas?pageNumber=1&pageSize=5");
        var resultPage1 = await responsePage1.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();

         - Segunda página com 5 itens
        var responsePage2 = await _client.GetAsync("/api/v1/vendas?pageNumber=2&pageSize=5");
        var resultPage2 = await responsePage2.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();

         - Terceira página com 5 itens
        var responsePage3 = await _client.GetAsync("/api/v1/vendas?pageNumber=3&pageSize=5");
        var resultPage3 = await responsePage3.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();

         - Página 1
        responsePage1.StatusCode.Should().Be(HttpStatusCode.OK);
        resultPage1.Should().NotBeNull();
        resultPage1!.Items.Should().HaveCount(5, "pageSize foi definido como 5");
        resultPage1.PageNumber.Should().Be(1);
        resultPage1.PageSize.Should().Be(5);
        resultPage1.TotalCount.Should().BeGreaterThanOrEqualTo(15, "deve ter pelo menos as 15 vendas criadas neste teste");
        resultPage1.HasPreviousPage.Should().BeFalse();
        resultPage1.HasNextPage.Should().BeTrue();

         - Página 2
        responsePage2.StatusCode.Should().Be(HttpStatusCode.OK);
        resultPage2.Should().NotBeNull();
        resultPage2!.Items.Should().HaveCount(5, "pageSize foi definido como 5");
        resultPage2.PageNumber.Should().Be(2);
        resultPage2.HasPreviousPage.Should().BeTrue();
        resultPage2.HasNextPage.Should().BeTrue();

         - Página 3
        responsePage3.StatusCode.Should().Be(HttpStatusCode.OK);
        resultPage3.Should().NotBeNull();
        resultPage3!.Items.Should().HaveCount(5, "pageSize foi definido como 5");
        resultPage3.PageNumber.Should().Be(3);
        resultPage3.HasPreviousPage.Should().BeTrue();

         - Vendas não devem se repetir entre páginas
        var idsPage1 = resultPage1.Items.Select(v => v.Id).ToList();
        var idsPage2 = resultPage2.Items.Select(v => v.Id).ToList();
        var idsPage3 = resultPage3.Items.Select(v => v.Id).ToList();
        
        idsPage1.Should().NotIntersectWith(idsPage2, "vendas não devem se repetir entre páginas");
        idsPage1.Should().NotIntersectWith(idsPage3, "vendas não devem se repetir entre páginas");
        idsPage2.Should().NotIntersectWith(idsPage3, "vendas não devem se repetir entre páginas");
    }

    [Fact]
    public async Task Get_Listagem_DeveRetornarMetadadosDePaginacao()
    {
        
        await CriarMultiplasVendas(7);

        
        var response = await _client.GetAsync("/api/v1/vendas?pageNumber=1&pageSize=3");

        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PagedResult<VendaDto>>();
        result.Should().NotBeNull();
        
        // Validar metadados de paginação
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(7, "deve ter pelo menos as 7 vendas criadas neste teste");
        result.PageNumber.Should().Be(1, "deve retornar o número da página solicitada");
        result.PageSize.Should().Be(3, "deve retornar o tamanho da página solicitado");
        result.TotalPages.Should().BeGreaterThanOrEqualTo(3, "com pelo menos 7 itens e pageSize 3, deve ter pelo menos 3 páginas");
        result.HasPreviousPage.Should().BeFalse("primeira página não tem página anterior");
        result.HasNextPage.Should().BeTrue("primeira página deve ter próxima página");
        
        // Validar que os itens estão presentes
        result.Items.Should().HaveCount(3, "pageSize foi definido como 3");
        result.Items.Should().OnlyContain(v => v.Id != Guid.Empty);
    }

    /// <summary>
    /// Método helper para criar múltiplas vendas.
    /// </summary>
    private async Task CriarMultiplasVendas(int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var request = _builder.GerarVendaValida(quantidadeItens: 1);
            var response = await _client.PostAsJsonAsync("/api/v1/vendas", request);
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Método helper para criar uma venda com um cliente específico.
    /// </summary>
    private async Task CriarVendaComCliente(Guid clienteId)
    {
        var request = new CriarVendaRequest(
            ClienteId: clienteId,
            FilialId: Guid.NewGuid(),
            Itens: _builder.GerarItens(1)
        );
        
        var response = await _client.PostAsJsonAsync("/api/v1/vendas", request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Método helper para criar uma venda com uma filial específica.
    /// </summary>
    private async Task CriarVendaComFilial(Guid filialId)
    {
        var request = new CriarVendaRequest(
            ClienteId: Guid.NewGuid(),
            FilialId: filialId,
            Itens: _builder.GerarItens(1)
        );
        
        var response = await _client.PostAsJsonAsync("/api/v1/vendas", request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Método helper para criar uma venda válida e retornar seu ID.
    /// </summary>
    private async Task<Guid> CriarVendaHelper()
    {
        var request = _builder.GerarVendaValida(quantidadeItens: 1);
        var response = await _client.PostAsJsonAsync("/api/v1/vendas", request);
        response.EnsureSuccessStatusCode();
        
        var vendaId = await response.Content.ReadFromJsonAsync<Guid>();
        return vendaId;
    }
}
