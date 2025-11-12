using MediatR;
using Venda.Application.DTOs;
using Venda.Application.Mappers;
using Venda.Application.Queries;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;

namespace Venda.Application.Handlers;

public class ListarVendasHandler : IRequestHandler<ListarVendasQuery, PagedResult<VendaDto>>
{
    private readonly IVendaRepository _repository;
    
    public ListarVendasHandler(IVendaRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    
    public async Task<PagedResult<VendaDto>> Handle(ListarVendasQuery request, CancellationToken cancellationToken)
    {
        //Parse status string para enum se fornecido
        StatusVenda? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status) && 
            Enum.TryParse<StatusVenda>(request.Status, true, out var statusEnum))
        {
            status = statusEnum;
        }
        
        var (vendas, totalCount) = await _repository.ListarComFiltrosAsync(
            request.PageNumber,
            request.PageSize,
            request.ClienteId,
            request.FilialId,
            status,
            request.DataInicio,
            request.DataFim,
            cancellationToken);
        
        var vendasDto = vendas.ToDto();
        
        return new PagedResult<VendaDto>(
            vendasDto,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
