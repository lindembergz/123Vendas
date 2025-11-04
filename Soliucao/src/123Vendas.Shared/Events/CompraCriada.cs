namespace _123Vendas.Shared.Events;

/// <summary>
/// Evento publicado quando uma nova venda é criada.
/// Consumido pelos módulos de Estoque e CRM para ajustes e histórico.
/// </summary>
public record CompraCriada : DomainEvent
{
    public Guid VendaId { get; init; }
    public int NumeroVenda { get; init; }
    public Guid ClienteId { get; init; }

    public CompraCriada(Guid vendaId, int numeroVenda, Guid clienteId)
    {
        VendaId = vendaId;
        NumeroVenda = numeroVenda;
        ClienteId = clienteId;
    }

    //Construtor para desserialização
    private CompraCriada() { }
}
