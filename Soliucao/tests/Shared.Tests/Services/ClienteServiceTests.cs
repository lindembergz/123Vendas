using _123Vendas.Shared.Interfaces;
using CRM.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using Xunit;

namespace Shared.Tests.Services;

public class ClienteServiceTests
{
    private readonly ILogger<ClienteService> _logger;
    private readonly HttpMessageHandlerStub _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IClienteService _clienteService;

    public ClienteServiceTests()
    {
        _logger = Substitute.For<ILogger<ClienteService>>();
        _httpMessageHandler = new HttpMessageHandlerStub();
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://crm-api.example.com")
        };
        _clienteService = new ClienteService(_httpClient, _logger);
    }

    [Fact]
    public async Task ClienteExisteAsync_QuandoClienteExiste_DeveRetornarTrue()
    {
        
        var clienteId = Guid.NewGuid();
        _httpMessageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);

        
        var resultado = await _clienteService.ClienteExisteAsync(clienteId);

        
        resultado.Should().BeTrue();
        _httpMessageHandler.LastRequestUri.Should().Be($"https://crm-api.example.com/api/v1/clientes/{clienteId}");
    }

    [Fact]
    public async Task ClienteExisteAsync_QuandoClienteNaoExiste_DeveRetornarFalse()
    {
        
        var clienteId = Guid.NewGuid();
        _httpMessageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.NotFound);

        
        var resultado = await _clienteService.ClienteExisteAsync(clienteId);

        
        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task ClienteExisteAsync_QuandoOcorreErroDeRede_DeveLancarHttpRequestException()
    {
        
        var clienteId = Guid.NewGuid();
        _httpMessageHandler.ExceptionToThrow = new HttpRequestException("Network error");

        
        var act = async () => await _clienteService.ClienteExisteAsync(clienteId);

        
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Network error");
    }

    [Fact]
    public async Task ClienteExisteAsync_QuandoOcorreTimeout_DeveLancarTaskCanceledException()
    {
        
        var clienteId = Guid.NewGuid();
        _httpMessageHandler.ExceptionToThrow = new TaskCanceledException("Request timeout");

        
        var act = async () => await _clienteService.ClienteExisteAsync(clienteId);

        
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
