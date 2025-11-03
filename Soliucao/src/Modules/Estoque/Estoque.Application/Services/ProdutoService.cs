using _123Vendas.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Estoque.Application.Services;

/// <summary>
/// Serviço de integração com Estoque para reserva de produtos.
/// Utiliza HttpClient injetado via IHttpClientFactory para evitar socket exhaustion.
/// </summary>
public class ProdutoService : IProdutoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProdutoService> _logger;

    /// <summary>
    /// Construtor que recebe HttpClient injetado pelo IHttpClientFactory.
    /// IMPORTANTE: NUNCA instanciar HttpClient diretamente com new HttpClient()
    /// </summary>
    public ProdutoService(HttpClient httpClient, ILogger<ProdutoService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> ReservarEstoqueAsync(Guid produtoId, int quantidade, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Reservando {Quantidade} unidades do produto {ProdutoId} no Estoque",
                quantidade,
                produtoId);

            // Simula chamada HTTP para serviço de Estoque
            // Em produção, seria uma chamada real: POST /api/v1/produtos/{produtoId}/reservar
            var request = new
            {
                produtoId,
                quantidade
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/produtos/{produtoId}/reservar",
                request,
                ct);

            var sucesso = response.IsSuccessStatusCode;

            if (sucesso)
            {
                _logger.LogInformation(
                    "Reserva de {Quantidade} unidades do produto {ProdutoId} realizada com sucesso",
                    quantidade,
                    produtoId);
            }
            else
            {
                _logger.LogWarning(
                    "Falha ao reservar {Quantidade} unidades do produto {ProdutoId}. Status: {StatusCode}",
                    quantidade,
                    produtoId,
                    response.StatusCode);
            }

            return sucesso;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Erro de rede ao reservar estoque do produto {ProdutoId}",
                produtoId);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(
                ex,
                "Timeout ao reservar estoque do produto {ProdutoId}",
                produtoId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro inesperado ao reservar estoque do produto {ProdutoId}",
                produtoId);
            throw;
        }
    }
}
