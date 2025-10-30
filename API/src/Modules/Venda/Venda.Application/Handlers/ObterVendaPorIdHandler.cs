using MediatR;
using Venda.Application.DTOs;
using Venda.Application.Queries;
using Venda.Domain.Aggregates;
using Venda.Domain.Interfaces;

namespace Venda.Application.Handlers;

public class ObterVendaPorIdHandler : IRequestHandler<ObterVendaPorIdQuery, VendaDto?>
{
    private readonly IVendaRepository _repository;
    
    public ObterVendaPorIdHandler(IVendaRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    
    public async Task<VendaDto?> Handle(ObterVendaPorIdQuery request, CancellationToken cancellationToken)
    {
        var venda = await _repository.ObterPorIdAsync(request.VendaId, cancellationToken);
        
        return venda != null ? MapearParaDto(venda) : null;
    }
    
    private static VendaDto MapearParaDto(VendaAgregado venda)
    {
        var itensDto = venda.Produtos
            .Select(item => new ItemVendaDto(
                item.ProdutoId,
                item.Quantidade,
                item.ValorUnitario,
                item.Desconto,
                item.Total))
            .ToList();
        
        return new VendaDto(
            venda.Id,
            venda.NumeroVenda,
            venda.Data,
            venda.ClienteId,
            Guid.TryParse(venda.Filial, out var filialId) ? filialId : Guid.Empty,
            venda.ValorTotal,
            venda.Status.ToString(),
            itensDto);
    }
}
