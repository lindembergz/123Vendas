using _123Vendas.Shared.Common;
using MediatR;
using Venda.Application.DTOs;

namespace Venda.Application.Commands;

public record CriarVendaCommand(
    Guid RequestId,
    Guid ClienteId,
    Guid FilialId,
    List<ItemVendaDto> Itens) : IRequest<Result<Guid>>;
