namespace Venda.Application.DTOs;

public record CriarVendaRequest(
    Guid ClienteId,
    Guid FilialId,
    List<ItemVendaDto> Itens);
