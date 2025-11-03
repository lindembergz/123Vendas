using _123Vendas.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Estoque.Application.Services;

/// <summary>
/// Implementação MOCK do serviço de Estoque para testes e desenvolvimento.
/// Sempre retorna que a reserva foi bem-sucedida.
/// </summary>
public class ProdutoServiceMock : IProdutoService
{
    private readonly ILogger<ProdutoServiceMock> _logger;

    public ProdutoServiceMock(ILogger<ProdutoServiceMock> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> ReservarEstoqueAsync(Guid produtoId, int quantidade, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] Reservando {Quantidade} unidades do produto {ProdutoId} - Sempre retorna TRUE",
            quantidade,
            produtoId);

        // MOCK: Sempre retorna que a reserva foi bem-sucedida
        return Task.FromResult(true);
    }
}
