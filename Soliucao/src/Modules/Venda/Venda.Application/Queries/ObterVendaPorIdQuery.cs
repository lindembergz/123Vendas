using MediatR;
using Venda.Application.DTOs;

namespace Venda.Application.Queries;

public record ObterVendaPorIdQuery(Guid VendaId) : IRequest<VendaDto?>;
