using _123Vendas.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace CRM.Application.Services;

/// <summary>
/// Implementação MOCK do serviço de CRM para testes e desenvolvimento.
/// Sempre retorna que o cliente existe.
/// </summary>
public class ClienteServiceMock : IClienteService
{
    private readonly ILogger<ClienteServiceMock> _logger;

    public ClienteServiceMock(ILogger<ClienteServiceMock> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> ClienteExisteAsync(Guid clienteId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] Verificando cliente {ClienteId} - Sempre retorna TRUE",
            clienteId);

        // MOCK: Sempre retorna que o cliente existe
        return Task.FromResult(true);
    }
}
