using MediatR;
using Venda.Application.DTOs;
using Venda.Application.Mappers;
using Venda.Application.Queries;
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
        
        return venda?.ToDto();
    }
}
