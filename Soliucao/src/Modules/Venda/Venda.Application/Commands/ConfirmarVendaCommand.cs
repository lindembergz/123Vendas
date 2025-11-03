using _123Vendas.Shared.Common;
using MediatR;

namespace Venda.Application.Commands;

public record ConfirmarVendaCommand(
    Guid RequestId,
    Guid VendaId) : IRequest<Result>;
