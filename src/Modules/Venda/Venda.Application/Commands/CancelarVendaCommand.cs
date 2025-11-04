using _123Vendas.Shared.Common;
using MediatR;

namespace Venda.Application.Commands;

public record CancelarVendaCommand(
    Guid RequestId,
    Guid VendaId) : IRequest<Result>;
