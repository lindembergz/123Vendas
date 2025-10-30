using _123Vendas.Shared.Common;
using MediatR;
using Venda.Application.DTOs;

namespace Venda.Application.Commands;

public record AtualizarVendaCommand(
    Guid RequestId,
    Guid VendaId,
    List<ItemVendaDto> Itens) : IRequest<Result<VendaDto>>;
