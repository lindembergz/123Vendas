using System.Text.Json.Serialization;

namespace _123Vendas.Shared.Events;

/// <summary>
/// Evento publicado quando uma venda existente é alterada.
/// Contém os IDs dos produtos que foram modificados para ajustes de estoque.
/// </summary>
public record CompraAlterada : DomainEvent
{
    public Guid VendaId { get; init; }
    public Guid[] ProdutosModificados { get; init; }

    public CompraAlterada(Guid vendaId, IEnumerable<Guid> produtosModificados)
    {
        VendaId = vendaId;
        ProdutosModificados = produtosModificados.ToArray();
    }

    // Construtor para desserialização JSON
    [JsonConstructor]
    public CompraAlterada(Guid vendaId, Guid[] produtosModificados)
    {
        VendaId = vendaId;
        ProdutosModificados = produtosModificados ?? Array.Empty<Guid>();
    }
}
