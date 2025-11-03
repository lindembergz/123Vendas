using MediatR;
using Venda.Application.DTOs;

namespace Venda.Application.Queries;

public record ListarVendasQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? ClienteId = null,
    Guid? FilialId = null,
    string? Status = null,
    DateTime? DataInicio = null,
    DateTime? DataFim = null) : IRequest<PagedResult<VendaDto>>;
