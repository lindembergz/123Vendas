namespace Venda.Application.DTOs;

public record VendaDto(
    Guid Id,
    int Numero,
    DateTime Data,
    Guid ClienteId,
    Guid FilialId,
    decimal ValorTotal,
    string Status,
    List<ItemVendaDto> Itens);
