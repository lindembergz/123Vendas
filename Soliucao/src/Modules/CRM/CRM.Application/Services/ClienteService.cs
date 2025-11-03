using _123Vendas.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace CRM.Application.Services;

/// <summary>
/// Serviço de integração com CRM para validação de clientes.
/// Utiliza HttpClient injetado via IHttpClientFactory para evitar socket exhaustion.
/// </summary>
public class ClienteService : IClienteService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClienteService> _logger;

    /// <summary>
    /// Construtor que recebe HttpClient injetado pelo IHttpClientFactory.
    /// IMPORTANTE: NUNCA instanciar HttpClient diretamente com new HttpClient()
    /// </summary>
    public ClienteService(HttpClient httpClient, ILogger<ClienteService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> ClienteExisteAsync(Guid clienteId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Verificando existência do cliente {ClienteId} no CRM", clienteId);

            // Simula chamada HTTP para serviço de CRM
            // Em produção, seria uma chamada real: GET /api/v1/clientes/{clienteId}
            var response = await _httpClient.GetAsync($"/api/v1/clientes/{clienteId}", ct);

            var existe = response.IsSuccessStatusCode;

            _logger.LogInformation(
                "Cliente {ClienteId} {Status} no CRM",
                clienteId,
                existe ? "encontrado" : "não encontrado");

            return existe;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Erro de rede ao verificar cliente {ClienteId} no CRM",
                clienteId);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(
                ex,
                "Timeout ao verificar cliente {ClienteId} no CRM",
                clienteId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro inesperado ao verificar cliente {ClienteId} no CRM",
                clienteId);
            throw;
        }
    }
}
