namespace _123Vendas.Shared.Events;

/// <summary>
/// Evento publicado quando uma venda é cancelada.
/// Consumido pelo módulo de Estoque para reversão de estoque.
/// </summary>
public record CompraCancelada : DomainEvent
{
    public Guid VendaId { get; init; }
    public string Motivo { get; init; }

    public CompraCancelada(Guid vendaId, string motivo)
    {
        VendaId = vendaId;
        Motivo = motivo ?? string.Empty;
    }

    //Construtor para desserialização
    private CompraCancelada()
    {
        Motivo = string.Empty;
    }
}
