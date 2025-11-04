using _123Vendas.Shared.Interfaces;
using CRM.Application.Services;
using Estoque.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using Xunit;

namespace Shared.Tests.Integration;

/// <summary>
/// Testes de integração para validar o comportamento do HttpClient com Polly
/// Estes testes demonstram como as políticas de Retry e Circuit Breaker funcionam
/// </summary>
public class HttpClientPollyIntegrationTests
{
    [Fact]
    public async Task ClienteService_ComRetryPolicy_DeveTentarNovamenteAposErroTransitorio()
    {
        
        var services = new ServiceCollection();
        services.AddLogging();
        
        var tentativas = 0;
        var handler = new TestHttpMessageHandler((request, ct) =>
        {
            tentativas++;
            
            // Simula erro transitório nas primeiras 2 tentativas
            if (tentativas <= 2)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
            
            // Sucesso na 3ª tentativa
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        services.AddHttpClient<IClienteService, ClienteService>(client =>
            {
                client.BaseAddress = new Uri("https://crm-api.example.com");
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(100)));

        var provider = services.BuildServiceProvider();
        var clienteService = provider.GetRequiredService<IClienteService>();

        
        var resultado = await clienteService.ClienteExisteAsync(Guid.NewGuid());

        
        resultado.Should().BeTrue();
        tentativas.Should().Be(3, "deve ter tentado 3 vezes antes de obter sucesso");
    }

    [Fact]
    public async Task ProdutoService_ComCircuitBreaker_DeveAbrirCircuitoApos5Falhas()
    {
        
        var services = new ServiceCollection();
        services.AddLogging();
        
        var tentativas = 0;
        var handler = new TestHttpMessageHandler((request, ct) =>
        {
            tentativas++;
            // Sempre retorna erro
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });

        services.AddHttpClient<IProdutoService, ProdutoService>(client =>
            {
                client.BaseAddress = new Uri("https://estoque-api.example.com");
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(1)));

        var provider = services.BuildServiceProvider();
        var produtoService = provider.GetRequiredService<IProdutoService>();

         //- Fazer 5 chamadas para abrir o circuito
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await produtoService.ReservarEstoqueAsync(Guid.NewGuid(), 1);
            }
            catch
            {
                // Ignora exceções das primeiras 5 tentativas
            }
        }

        // A 6ª chamada deve lançar BrokenCircuitException
        var act = async () => await produtoService.ReservarEstoqueAsync(Guid.NewGuid(), 1);

        
        await act.Should().ThrowAsync<Polly.CircuitBreaker.BrokenCircuitException>(
            "o circuito deve estar aberto após 5 falhas consecutivas");
        
        tentativas.Should().Be(5, "deve ter feito apenas 5 tentativas antes de abrir o circuito");
    }

    [Fact]
    public async Task ClienteService_ComTimeout_DeveLancarTaskCanceledException()
    {
        
        var services = new ServiceCollection();
        services.AddLogging();
        
        var handler = new TestHttpMessageHandler(async (request, ct) =>
        {
            // Simula operação lenta
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        services.AddHttpClient<IClienteService, ClienteService>(client =>
        {
            client.BaseAddress = new Uri("https://crm-api.example.com");
            client.Timeout = TimeSpan.FromMilliseconds(100); // Timeout muito curto
        })
        .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        var clienteService = provider.GetRequiredService<IClienteService>();

        
        var act = async () => await clienteService.ClienteExisteAsync(Guid.NewGuid());

        
        await act.Should().ThrowAsync<TaskCanceledException>(
            "a requisição deve ser cancelada por timeout");
    }

    // Helper class para criar HttpMessageHandler customizado para testes
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        public TestHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _sendAsync(request, cancellationToken);
        }
    }
}
