using Venda.Domain.Aggregates;
using Venda.Domain.Enums;

namespace Venda.Domain.Interfaces;

public interface IVendaRepository
{
    Task<VendaAgregado?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<VendaAgregado>> ListarAsync(CancellationToken ct = default);
    Task<(List<VendaAgregado> Items, int TotalCount)> ListarComFiltrosAsync(
        int pageNumber,
        int pageSize,
        Guid? clienteId = null,
        Guid? filialId = null,
        StatusVenda? status = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        CancellationToken ct = default);
    Task AdicionarAsync(VendaAgregado venda, CancellationToken ct = default);
    Task AtualizarAsync(VendaAgregado venda, CancellationToken ct = default);
    Task<bool> ExisteAsync(Guid id, CancellationToken ct = default);
    Task<int> ObterUltimoNumeroPorFilialAsync(Guid filialId, CancellationToken ct = default);
}
