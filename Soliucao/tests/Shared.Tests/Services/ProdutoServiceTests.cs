using _123Vendas.Shared.Interfaces;
using Estoque.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using Xunit;

namespace Shared.Tests.Services;

public class ProdutoServiceTests
{
    private readonly ILogger<ProdutoService> _logger;
    private readonly HttpMessageHandlerStub _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IProdutoService _produtoService;

    public ProdutoServiceTests()
    {
        _logger = Substitute.For<ILogger<ProdutoService>>();
        _httpMessageHandler = new HttpMessageHandlerStub();
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://estoque-api.example.com")
        };
        _produtoService = new ProdutoService(_httpClient, _logger);
    }

    [Fact]
    public async Task ReservarEstoqueAsync_QuandoReservaComSucesso_DeveRetornarTrue()
    {
        
        var produtoId = Guid.NewGuid();
        var quantidade = 5;
        _httpMessageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);

        
        var resultado = await _produtoService.ReservarEstoqueAsync(produtoId, quantidade);

        
        resultado.Should().BeTrue();
        _httpMessageHandler.LastRequestUri.Should()
            .Be($"https://estoque-api.example.com/api/v1/produtos/{produtoId}/reservar");
    }

    [Fact]
    public async Task ReservarEstoqueAsync_QuandoEstoqueInsuficiente_DeveRetornarFalse()
    {
        
        var produtoId = Guid.NewGuid();
        var quantidade = 100;
        _httpMessageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.BadRequest);

        
        var resultado = await _produtoService.ReservarEstoqueAsync(produtoId, quantidade);

        
        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task ReservarEstoqueAsync_QuandoProdutoNaoExiste_DeveRetornarFalse()
    {
        
        var produtoId = Guid.NewGuid();
        var quantidade = 5;
        _httpMessageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.NotFound);

        
        var resultado = await _produtoService.ReservarEstoqueAsync(produtoId, quantidade);

        
        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task ReservarEstoqueAsync_QuandoOcorreErroDeRede_DeveLancarHttpRequestException()
    {
        
        var produtoId = Guid.NewGuid();
        var quantidade = 5;
        _httpMessageHandler.ExceptionToThrow = new HttpRequestException("Network error");

        
        var act = async () => await _produtoService.ReservarEstoqueAsync(produtoId, quantidade);

        
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Network error");
    }

    [Fact]
    public async Task ReservarEstoqueAsync_QuandoOcorreTimeout_DeveLancarTaskCanceledException()
    {
        
        var produtoId = Guid.NewGuid();
        var quantidade = 5;
        _httpMessageHandler.ExceptionToThrow = new TaskCanceledException("Request timeout");

        
        var act = async () => await _produtoService.ReservarEstoqueAsync(produtoId, quantidade);

        
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    // Helper class para mockar HttpMessageHandler
    private class HttpMessageHandlerStub : HttpMessageHandler
    {
        public HttpResponseMessage? ResponseToReturn { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(ResponseToReturn ?? new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
