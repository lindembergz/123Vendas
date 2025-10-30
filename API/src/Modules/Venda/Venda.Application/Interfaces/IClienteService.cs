namespace Venda.Application.Interfaces;

public interface IClienteService
{
    Task<bool> ClienteExisteAsync(Guid clienteId, CancellationToken ct = default);
}
